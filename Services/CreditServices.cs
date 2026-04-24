using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backendTimeBank.Models;
using backendTimeBank.Models.DTOS;
using backendTimeBank.Services;
using BlogApiPrev.Context;
using BlogApiPrev.Models;
using Microsoft.EntityFrameworkCore;




namespace backendTimeBank.Services
{
    public class CreditServices
    {
        private readonly DataContext _context;


        public CreditServices(DataContext context)
        {
            _context = context;
        }


        public async Task<bool> Transfer(TransactionDTO transactionDTO)
{
    UserModel? sender = await GetUserInfoByUserIdAsync(transactionDTO.SenderId);
    int receiverId = await GetUserIdByUsername(transactionDTO.ReceiverUsername);
    UserModel? receiver = await GetUserInfoByUserIdAsync(receiverId);

    if (sender == null || receiver == null)
        return false;

    int amount = transactionDTO.Amount;

    // Validate amount
    if (amount <= 0)
        return false;

    if (sender.Credits < amount)
        return false;

    sender.Credits -= amount;
    receiver.Credits += amount;

    TransactionModel transaction = new TransactionModel
    {
        SenderId = sender.Id,
        SenderUser = sender.Username,
        ReceiverId = receiverId,
        ReceiverUser = receiver.Username,
        SenderCredits = sender.Credits,
        ReceiverCredits = receiver.Credits
    };

    _context.Users.Update(sender);
    _context.Users.Update(receiver);
    _context.Transactions.Add(transaction);

    await _context.SaveChangesAsync();

    return true;
}




        public async Task<int> GetUserIdByUsername(string username)
        {
            var userInfo =  await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);
            return userInfo.Id;
        }




        public async Task<List<TransactionModel>> GetTransactions(int userId)
        {
            return await _context.Transactions
                .Where(r =>
                    r.SenderId == userId || r.ReceiverId == userId)
                .ToListAsync();
        }


        public async Task<UserModel?> GetUserInfoByUserIdAsync(int userId) => await _context.Users.SingleOrDefaultAsync(user => user.Id == userId);

        public async Task<bool> DoesUserExist(string username)
        {
            return await _context.Users.AnyAsync(user => user.Username == username);
        }
    }
}
