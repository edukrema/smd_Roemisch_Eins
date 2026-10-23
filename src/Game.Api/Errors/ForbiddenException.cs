namespace Game.Api.Errors;

public class ForbiddenException(string message) : Exception(message);
