using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace BlogApiPrev.Models
{
    public class SignalRequest
    {
        public string? UserId { get; set; }
        public string? Message { get; set; }
    }
}
