namespace Game.Api.Errors;

public class NotFoundException(string message) : Exception(message);
