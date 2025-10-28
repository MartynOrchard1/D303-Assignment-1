using SQLite;
using TuckBox.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TuckBox.Data;

public class AppDb
{
    private readonly SQLiteAsyncConnection _db;

    public AppDb(string dbPath) => _db = new SQLiteAsyncConnection(dbPath);


    public async Task InitAsync()
    {
        await _db.CreateTableAsync<User>();
        await _db.CreateTableAsync<DeliveryAddress>();
        await _db.CreateTableAsync<City>();
        await _db.CreateTableAsync<TimeSlot>();
        await _db.CreateTableAsync<Food>();
        await _db.CreateTableAsync<Food_Extra_Details>();
        await _db.CreateTableAsync<Order>();
        await _db.CreateTableAsync<User>();
    }

    // Insert helpers
    public Task<int> UpsertUserProfileAsync(User user)
    {
        return _db.InsertOrReplaceAsync(user);
    }

    public Task<User?> GetUserProfileAsync(string uid)
    {
        return _db.FindAsync<User>(uid);
    }

    public SQLiteAsyncConnection Conn => _db;
}

