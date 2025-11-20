using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Listens to movement input event, calculates desired location, and moves player. Emits movement start/end events.
/// </summary>
public class PlayerMovement : MonoBehaviour {
    private Vector3Int CurrentPlayerCoords => Vector3Int.RoundToInt(transform.position);
    
    // REFERENCES
    // Simply assigned in the inspector for the prototype. 
    [SerializeField] private Tilemap currentLevelTilemap;
    
    // STATE
    [SerializeField] private bool isMovementAttemptOngoing;
    private readonly TileBase[] currentRelevantTiles = new TileBase[50];
    
    // AUTHORING
    private const int MaxMovementTileLength = 50;
    [SerializeField] private float movementTimePerTile;
    [SerializeField] private TileBase nonNavigableTile;
    [SerializeField] private TileBase navigableTile;

    // DELEGATES
    public delegate void PlayerStartedMovement(CardinalDirection directionOfMovement);
    public static event PlayerStartedMovement OnPlayerStartedMovement;
    public delegate void PlayerEndedMovement();
    public static event PlayerEndedMovement OnPlayerEndedMovement;
    
    #region Lifetime

    private void OnEnable() {
        if (currentLevelTilemap == null)
            throw new NullReferenceException("Please assign a tilemap to the player's movement component.");
        PlayerInput.OnPlayerInputReceived += AttemptMovement;
    }

    private void OnDisable() => PlayerInput.OnPlayerInputReceived -= AttemptMovement;

    #endregion

    #region Movement Logic
    
    /// <summary>
    /// Attempts to move the player in the input direction,
    /// given a valid tile is found and the player isn't already moving.
    /// </summary>
    /// <param name="input"></param>
    private void AttemptMovement(Vector2 input) {
        if (isMovementAttemptOngoing)
            return;
        isMovementAttemptOngoing = true;

        var desiredDirection = CardinalDirectionUtils.GetCardinalDirectionFromInput(input);
        var targetCoords = GetPlayerDestination(desiredDirection);
        MovePlayerToCoords(targetCoords, desiredDirection);
    }

    /// <summary>
    /// Determines where the player should move to, as if sliding on ice until they hit a wall.  
    /// Given a cardinal direction, scans the level tilemap in that direction from the player's location
    /// and finds the farthest tile in that direction the player can move to.
    ///
    /// In this example, the player moves from B5 to B1 to F1 to F3,
    /// each time traveling until they hit a non-navigable tile.
    ///  
    ///         0 1 2 3 4 5 6
    ///       A ┌───────────┐
    ///       B │ 2◄──────1 │
    ///       C │ │         │
    ///       D │ │     ┌───┘
    ///       E │ ▼     │    
    ///       F │ 3──►4 │    
    ///       G └───────┘    
    ///
    /// </summary>
    /// <param name="desiredDirection">the cardinal direction the player wants to move</param>
    /// <returns>the coordinates of the farthest navigable tile from the player in the desired direction</returns>
    private Vector2Int GetPlayerDestination(CardinalDirection desiredDirection) {
        // initialize and clear variables
        Array.Clear(currentRelevantTiles, 0, MaxMovementTileLength);
        int sizeX, sizeY;
        int xMin, yMin;
        
        // gather initial data about desired movement
        var edgeOfLevelCoords = GetEdgeOfLevelCoordinates(CurrentPlayerCoords, desiredDirection);
        var isHorizontal = CardinalDirectionUtils.IsHorizontal(desiredDirection);
        var isPositive = CardinalDirectionUtils.IsPositive(desiredDirection);
        // this offset prevents off-by-1 errors when sampling the backwards from the player's position
        var directionalOffset = CardinalDirectionUtils.IsPositive(desiredDirection) ? 0 : 1; 
        
        // create the bounds with which to sample the tilemap
        if (isHorizontal) {
            sizeX = Mathf.Abs(edgeOfLevelCoords.x - CurrentPlayerCoords.x) + directionalOffset;
            sizeY = 1;
            xMin = Mathf.Min(CurrentPlayerCoords.x, edgeOfLevelCoords.x);
            yMin = CurrentPlayerCoords.y;
        }
        else {
            sizeX = 1;
            sizeY = Mathf.Abs(edgeOfLevelCoords.y - CurrentPlayerCoords.y) + directionalOffset;
            xMin = CurrentPlayerCoords.x;
            yMin = Mathf.Min(CurrentPlayerCoords.y, edgeOfLevelCoords.y);
        }
        var bounds = new BoundsInt(xMin, yMin, zMin: 0, sizeX, sizeY, sizeZ: 1);
        
        // Sample the tilemap
        // For the first move in the diagram above, the array would be:
        // [non-navigable, navigable, navigable, navigable, navigable, navigable]
        //     B0             B1         B2         B3         B4         B5 
        // Note that the player's initial location in that example is B5.
        // The player's initial tile is included in the array.  
        currentLevelTilemap.GetTilesBlockNonAlloc(bounds, currentRelevantTiles);
        
        // Set up data to use when reading the array data.
        int indexArray;
        var xSampleCoord = CurrentPlayerCoords.x;
        var ySampleCoord = CurrentPlayerCoords.y;
        var xDirection = CardinalDirectionUtils.XDirection(desiredDirection);
        var yDirection = CardinalDirectionUtils.YDirection(desiredDirection);
        
        // Determine what index of the tile array to start on.
        // The array always returns tiles left-to-right, bottom-to-top.
        // If we're moving a "negative" direction (south or west), 
        // we'll jump to that array index and work backwards.
        if (desiredDirection is CardinalDirection.South)
            indexArray = Mathf.Abs(edgeOfLevelCoords.y - ySampleCoord);
        else if (desiredDirection is CardinalDirection.West)
            indexArray = Mathf.Abs(edgeOfLevelCoords.x - xSampleCoord);
        else
            indexArray = 0;
        var indexDirection = isPositive ? 1 : -1;

        // Step through the tiles in block until a non-navigable tile is reached
        for (; IsRelevantCoordWithinLevelBounds(); 
             xSampleCoord += xDirection, 
             ySampleCoord += yDirection, 
             indexArray += indexDirection) {
            if (currentRelevantTiles[indexArray] == navigableTile) continue;
            
            // If we've reached a tile that is non-navigable and we're not still at the player's position,
            // step backward in the relevant direction and break. 
            if (isHorizontal && xSampleCoord != CurrentPlayerCoords.x)
                xSampleCoord -= xDirection;
            else if(!isHorizontal && ySampleCoord != CurrentPlayerCoords.y)
                ySampleCoord -= yDirection;
            break;
        }
        
        return new Vector2Int(xSampleCoord, ySampleCoord);
        
        bool IsRelevantCoordWithinLevelBounds() {
            switch (desiredDirection) {
                case CardinalDirection.North: return ySampleCoord <= edgeOfLevelCoords.y; 
                case CardinalDirection.South: return ySampleCoord >= edgeOfLevelCoords.y;
                case CardinalDirection.East: return xSampleCoord <= edgeOfLevelCoords.x;
                case CardinalDirection.West: return xSampleCoord >= edgeOfLevelCoords.x;
            }
            throw new InvalidDirectionException();
        }

        Vector3Int GetEdgeOfLevelCoordinates(Vector3Int playerCoordinates, CardinalDirection cardinalDirection) {
            var edgeCoords = playerCoordinates;

            if (cardinalDirection is CardinalDirection.East)
                edgeCoords.x = currentLevelTilemap.cellBounds.xMax;
            else if (cardinalDirection is CardinalDirection.West)
                edgeCoords.x = currentLevelTilemap.cellBounds.xMin;
            else if (cardinalDirection is CardinalDirection.North)
                edgeCoords.y = currentLevelTilemap.cellBounds.yMax;
            else if (cardinalDirection is CardinalDirection.South)
                edgeCoords.y = currentLevelTilemap.cellBounds.yMin;

            return edgeCoords;
        }
    }

    private void MovePlayerToCoords(Vector2Int coords, CardinalDirection desiredDirection) {
        OnPlayerStartedMovement?.Invoke(desiredDirection);
        
        var totalTime = Mathf.Abs(coords.x - CurrentPlayerCoords.x) + Mathf.Abs(coords.y - CurrentPlayerCoords.y);
        transform
            .DOMove(new Vector3(coords.x, coords.y, transform.position.z), totalTime * movementTimePerTile)
            .SetEase(Ease.InQuad)
            .OnComplete(CompleteMove);
    }

    private void CompleteMove() {
        OnPlayerEndedMovement?.Invoke();
        isMovementAttemptOngoing = false;
    }
    
    #endregion
}