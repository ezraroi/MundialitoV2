using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Mundialito.Models;

// Models returned by AccountController actions.

public class ExternalLoginViewModel
{
    public string Name { get; set; }

    public string Url { get; set; }

    public string State { get; set; }
}

public class ManageInfoViewModel
{
    public string LocalLoginProvider { get; set; }

    public string UserName { get; set; }

    public IEnumerable<UserLoginInfoViewModel> Logins { get; set; }

    public IEnumerable<ExternalLoginViewModel> ExternalLoginProviders { get; set; }
}

public class UserInfoViewModel
{
    [JsonPropertyName("userName")]
    public string? UserName { get; set; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("roles")]
    public string Roles { get; set; }


    
}

internal class JsonPropertyAttribute : Attribute
{
}

public class UserLoginInfoViewModel
{
    public string LoginProvider { get; set; }

    public string ProviderKey { get; set; }
}

