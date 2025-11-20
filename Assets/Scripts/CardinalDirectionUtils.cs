using System;
using UnityEngine;

public enum CardinalDirection { North = 0, South, East, West, None }

public static class CardinalDirectionUtils {
    public static CardinalDirection GetCardinalDirectionFromInput(Vector2 input) {
        if (input.x > 0) return CardinalDirection.East;
        if (input.x < 0) return CardinalDirection.West;
        if (input.y > 0) return CardinalDirection.North;
        if (input.y  < 0) return CardinalDirection.South;
        return CardinalDirection.None;
    }

    public static bool IsPositive(CardinalDirection direction) {
        return direction switch {
            CardinalDirection.None => throw new InvalidDirectionException(),
            CardinalDirection.South or CardinalDirection.West => false,
            _ => true
        };
    }
    
    public static bool IsHorizontal(CardinalDirection direction) {
        if (direction is CardinalDirection.None)
            throw new InvalidDirectionException();
        return direction is CardinalDirection.West or CardinalDirection.East;
    }

    public static int XDirection(CardinalDirection direction) =>
        direction switch {
            CardinalDirection.West => -1,
            CardinalDirection.East => 1,
            _ => 0
        };
    
    public static int YDirection(CardinalDirection direction) =>
        direction switch {
            CardinalDirection.South => -1,
            CardinalDirection.North => 1,
            _ => 0
        };
}

public class InvalidDirectionException : Exception { }
