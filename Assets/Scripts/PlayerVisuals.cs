using System;
using UnityEngine;

/// <summary>
/// Listens to events from PlayerMovement and updates player visuals
/// </summary>
public class PlayerVisuals : MonoBehaviour {
    // ANIMATOR HASH
    private static readonly int Moving = Animator.StringToHash("Moving");

    // AUTHORING
    [SerializeField] private Animator animator;
    [SerializeField] private Transform playerVisualsTransform;
    [Tooltip("North = 0, South = 1, East = 2, West = 3")]
    [SerializeField] private float[] spriteRotationsPerDirection;
    
    #region Lifetime
    
    private void Start() {
        ValidateAuthoring();
        return;
        
        void ValidateAuthoring() {
            if (animator == null)
                throw new MissingReferenceException("Animator not assigned in player prefab.");
            if (playerVisualsTransform == null)
                throw new MissingReferenceException("PlayerVisuals not assigned in player prefab.");
            if (spriteRotationsPerDirection.Length != 4)
                throw new InvalidAuthoringException("SpriteRotationsPerDirection array must have 4 elements. See tooltip.");
        }
    }
    
    private void OnEnable() {
        PlayerMovement.OnPlayerStartedMovement += ShowPlayerMovementVisuals;
        PlayerMovement.OnPlayerEndedMovement += ShowPlayerStoppedVisuals;
    }

    private void OnDisable() {
        PlayerMovement.OnPlayerStartedMovement -= ShowPlayerMovementVisuals;
        PlayerMovement.OnPlayerEndedMovement -= ShowPlayerStoppedVisuals;
    }
    
    #endregion

    #region Visuals
    
    private void ShowPlayerMovementVisuals(CardinalDirection directionOfMovement) {
        if (playerVisualsTransform) 
            playerVisualsTransform.eulerAngles = new Vector3(0, 0, spriteRotationsPerDirection[(int)directionOfMovement]);
        if (animator) 
            animator.SetBool(Moving, true);
    }

    private void ShowPlayerStoppedVisuals() {
        if (animator) 
            animator.SetBool(Moving, false);
    }

    #endregion
}

internal class InvalidAuthoringException : Exception {
    public InvalidAuthoringException(string message) : base(message) { }
}

