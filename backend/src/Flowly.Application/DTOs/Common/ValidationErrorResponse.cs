using System;

namespace Flowly.Application.DTOs.Common;

public class ValidationErrorResponse : ErrorResponse
{
    
    public Dictionary<string, string[]> Errors { get; set; } = new();
}
