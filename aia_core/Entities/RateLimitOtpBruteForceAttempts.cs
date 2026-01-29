using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Entities
{
    
    [Table("RateLimitOtpBruteForceAttempts")]
    public class RateLimitOtpBruteForceAttempts
    {
        [Key]
        [Column("Id")]
        public Guid Id { get; set; }

        [Column("UserIdentifier")]
        public string UserIdentifier { get; set; }

        [Column("OtpCode")]
        public string OtpCode { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; }

        [Column("RateLimitOtpType")]
        public string? RateLimitOtpType { get; set; }

        [Column("IpAddress")]
        public string? IpAddress { get; set; }

        [Column("IsSuccess")]
        public bool IsSuccess { get; set; }
    }
}
