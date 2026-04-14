using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace backendTimeBank.Models.DTOS
{
    public class TransactionDTO
    {
        public int Id { get; set; }
        public int SenderId {get; set;}
        public string? ReceiverUsername {get; set;}
    }
}
