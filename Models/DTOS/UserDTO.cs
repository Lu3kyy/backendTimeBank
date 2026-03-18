using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlogApiPrev.Models.DTOS
{
    public class UserDTO
    {
        public string Username {get; set;} = string.Empty;
        public string Password {get; set;} = string.Empty;
    }
}