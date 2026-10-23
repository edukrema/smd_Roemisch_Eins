namespace Game.Domain;

/// <summary>An input is invalid (Api: 400).</summary>
public class ValidationException(string message) : Exception(message);
