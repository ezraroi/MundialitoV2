using System.ComponentModel.DataAnnotations;

namespace Mundialito.Models;

public class GoogleSigninModel
 {
        [Required]

        public string Credential { get; set; } 
 }