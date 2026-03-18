using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlogApiPrev.Models.DTOS
{
    public class UserInfoDTO
    {
        public int Id {get; set;}
        public string Username {get; set;} = string.Empty;
        public string? Name {get; set;}
        public string? ProfilePictureUrl {get; set;}
        public string? Description {get; set;}
    }
}