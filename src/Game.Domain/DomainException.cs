namespace Game.Domain;

/// <summary>A game rule forbids the action in the current state (Api: 409).</summary>
public class DomainException(string message) : Exception(message);
