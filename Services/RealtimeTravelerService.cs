using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using Safarion.Models;

namespace Safarion.Services
{
    public static class RealtimeTravelerService
    {
        private const string DatabaseUrl = "https://safarion-live-default-rtdb.firebaseio.com/";
        private static readonly FirebaseClient firebase = new FirebaseClient(DatabaseUrl);

        // ─────────────────────────────────────────────
        // USER ID & NAME
        // ─────────────────────────────────────────────

        public static string GetMyUserId()
        {
            var id = Preferences.Get("my_unique_id", string.Empty);
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString().Substring(0, 8);
                Preferences.Set("my_unique_id", id);
            }
            return id;
        }

        public static string GetMyName()
        {
            return Preferences.Get("UserName", DeviceInfo.Name);
        }

        // ─────────────────────────────────────────────
        // LOCATION
        // ─────────────────────────────────────────────

        public static async Task UploadMyLocation(Location location, string status)
        {
            try
            {
                string userId = GetMyUserId();

                var me = new NearbyTraveler
                {
                    Name = GetMyName(),
                    Status = status,
                    AvatarUrl = "user_icon.png",
                    Latitude = location.Latitude,
                    Longitude = location.Longitude
                };

                await firebase
                    .Child("Travelers")
                    .Child(userId)
                    .PutAsync(me);
            }
            catch { }
        }

        public static async Task<Dictionary<string, NearbyTraveler>> GetAllTravelers()
        {
            try
            {
                var list = await firebase
                    .Child("Travelers")
                    .OnceAsync<NearbyTraveler>();

                var dict = new Dictionary<string, NearbyTraveler>();

                foreach (var item in list)
                {
                    if (item.Object == null) continue;

                    dict[item.Key] = new NearbyTraveler
                    {
                        Name = item.Object.Name,
                        Status = item.Object.Status,
                        Latitude = item.Object.Latitude,
                        Longitude = item.Object.Longitude
                    };
                }

                return dict;
            }
            catch
            {
                return new Dictionary<string, NearbyTraveler>();
            }
        }

        // ─────────────────────────────────────────────
        // FRIEND SYSTEM
        // ─────────────────────────────────────────────

        public static async Task SendFriendRequest(string toUserId)
        {
            string myId = GetMyUserId();
            string myName = GetMyName();

            if (await IsAlreadyFriend(toUserId))
                throw new Exception("Already friends");

            await firebase
                .Child("Requests")
                .Child(toUserId)
                .Child(myId)
                .PutAsync(new
                {
                    SenderName = myName,   // ✅ FIXED
                    Status = "Pending"
                });
        }

        public static async Task<List<FriendRequest>> GetFriendRequests()
        {
            try
            {
                string myId = GetMyUserId();

                var list = await firebase
                    .Child("Requests")
                    .Child(myId)
                    .OnceAsync<FriendRequest>();

                return list.Select(x => new FriendRequest
                {
                    SenderId = x.Key,
                    SenderName = x.Object?.SenderName ?? "Unknown",
                    Status = x.Object?.Status ?? "Pending"
                }).ToList();
            }
            catch
            {
                return new List<FriendRequest>();
            }
        }

        public static async Task DeclineFriendRequest(string friendId)
        {
            string myId = GetMyUserId();
            await firebase
                .Child("Requests")
                .Child(myId)
                .Child(friendId)
                .DeleteAsync();
        }

        public static IObservable<FirebaseEvent<FriendRequest>> ListenToFriends()
        {
            string myId = GetMyUserId();

            return firebase
                .Child("Friends")
                .Child(myId)
                .AsObservable<FriendRequest>();
        }

        public static async Task AcceptFriendRequest(string friendId, string friendName)
        {
            string myId = GetMyUserId();
            string myName = GetMyName();

            var friendData = new { SenderName = friendName, Status = "Accepted" };
            var myData = new { SenderName = myName, Status = "Accepted" };

            await firebase.Child("Friends").Child(myId).Child(friendId).PutAsync(friendData);
            await firebase.Child("Friends").Child(friendId).Child(myId).PutAsync(myData);

            await firebase.Child("Requests").Child(myId).Child(friendId).DeleteAsync();
        }

        public static async Task<bool> AreWeFriends(string otherUserId)
        {
            try
            {
                string myId = GetMyUserId();

                var check = await firebase
                    .Child("Friends")
                    .Child(myId)
                    .Child(otherUserId)
                    .OnceSingleAsync<object>();

                return check != null;
            }
            catch
            {
                return false;
            }
        }

        // ✅ FIXED VERSION
        public static async Task<bool> IsAlreadyFriend(string targetUserId)
        {
            try
            {
                if (string.IsNullOrEmpty(targetUserId)) return false;

                string myId = GetMyUserId();

                var data = await firebase
                    .Child("Friends")      // ✅ FIXED PATH
                    .Child(myId)
                    .Child(targetUserId)
                    .OnceSingleAsync<object>();

                return data != null;
            }
            catch
            {
                return false;
            }
        }

        // ─────────────────────────────────────────────
        // CHAT SYSTEM
        // ─────────────────────────────────────────────

        public static string GetChatRoomId(string otherUserId)
        {
            string myId = GetMyUserId();
            var list = new List<string> { myId, otherUserId };
            list.Sort();
            return $"{list[0]}_{list[1]}";
        }

        public static async Task SendMessage(string otherUserId, string messageText)
        {
            string roomId = GetChatRoomId(otherUserId);

            var msg = new
            {
                SenderId = GetMyUserId(),
                SenderName = GetMyName(),
                Text = messageText,
                Time = DateTime.Now.ToString("HH:mm")
            };

            await firebase
                .Child("Chats")
                .Child(roomId)
                .PostAsync(msg);
        }

        public static IObservable<FirebaseEvent<ChatPayload>> ListenToChat(string otherUserId)
        {
            string roomId = GetChatRoomId(otherUserId);

            return firebase
                .Child("Chats")
                .Child(roomId)
                .AsObservable<ChatPayload>();
        }

        public class ChatPayload
        {
            public string SenderId { get; set; }
            public string SenderName { get; set; }
            public string Text { get; set; }
            public string Time { get; set; }
        }
    }

    public class FriendRequest
    {
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string Status { get; set; }
    }
}