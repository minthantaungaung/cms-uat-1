using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Entities
{
    public partial class PushNotificationLog
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string PushToken { get; set; }
        public string DeviceType { get; set; }
        public string DeviceModel { get; set; }
        public string NotificationId { get; set; }
        public string NotiType { get; set; }
        public DateTime? SentOn { get; set; }
        public DateTime? CreatedOn { get; set; }
        public bool IsSendSuccess { get; set; }
        public string FirebaseResult { get; set; }
    }
}
