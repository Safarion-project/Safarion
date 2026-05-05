using SQLite;
using Safarion.Models;

namespace Safarion.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection? _database;

        async Task Init()
        {
            if (_database != null)
                return;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "Safarion.db");
            _database = new SQLiteAsyncConnection(dbPath);

            // Create BOTH tables
            await _database.CreateTableAsync<UserProfile>();
            await _database.CreateTableAsync<EmergencyContact>();
        }

        // --- USER PROFILE METHODS ---
        public async Task<int> SaveUserAsync(UserProfile user)
        {
            await Init();
            if (user.Id != 0)
                return await _database!.UpdateAsync(user);
            else
                return await _database!.InsertAsync(user);
        }

        public async Task<UserProfile> GetUserAsync()
        {
            await Init();
            return await _database!.Table<UserProfile>().FirstOrDefaultAsync();
        }

        // --- CONTACT METHODS ---
        public async Task<List<EmergencyContact>> GetContactsAsync()
        {
            await Init();
            return await _database!.Table<EmergencyContact>().ToListAsync();
        }

        public async Task<int> SaveContactAsync(EmergencyContact contact)
        {
            await Init();
            return await _database!.InsertAsync(contact);
        }

        public async Task<int> DeleteContactAsync(EmergencyContact contact)
        {
            await Init();
            return await _database!.DeleteAsync(contact);
        }
    }
}