using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backendTimeBank.Models;
using backendTimeBank.Models.DTOS;
using backendTimeBank.Services;
using BlogApiPrev.Models;
using Microsoft.AspNetCore.Mvc;


namespace backendTimeBank.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CreditController : ControllerBase
    {
        private readonly CreditServices _services;
        public CreditController(CreditServices services)
        {
            _services = services;
        }


        [HttpPut("Transfer")]
        public async Task<ActionResult<bool>> Transfer(TransactionDTO transactionDTO)
        {
            var result = await _services.Transfer(transactionDTO);
            return Ok(result);
        }


        [HttpGet("GetTransactions")]
        public async Task<ActionResult<List<TransactionModel>>> GetTransactions(int userId)
        {
            var transactions = await _services.GetTransactions(userId);
            return Ok(transactions);
        }

        [HttpGet("GetUserIdByUsername/{username}")]
        public async Task<ActionResult<int>> GetUserIdByUsername(string username)
        {
            var userId = await _services.GetUserIdByUsername(username);
            return Ok(userId);
        }

        [HttpGet("DoesUserExist/{username}")]

        
        public async Task<ActionResult<bool>> DoesUserExist(string username)
        {
            var existence = await _services.DoesUserExist(username);
            return Ok(existence);
        }

    }
}

