using System;
using System.ComponentModel.DataAnnotations;
public class LoginDto()
{[Required(ErrorMessage = "Логин обязателен.")]
[StringLength(30, MinimumLength = 3)]
[RegularExpression(@"^[a-zA-Z0-9_\.]+$")]   
    public string? Login { get; set; }
    [Required(ErrorMessage = "Пароль обязателен.")]
    [StringLength(100, MinimumLength = 8)]
    public string? password{get;set;}
}

public class RefreshRequest
{

    public string? RefreshToken { get; set; }
}

