using System;

namespace Flowly.Application.DTOs.Common;

public class ValidationErrorResponse : ErrorResponse
{
    // Dictionary of field errors (fieldName -> error message)
    public Dictionary<string, string[]> Errors { get; set; } = new();
}
