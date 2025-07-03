using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailConsumerMQ.Model
{
    public class EmailLog
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string Subject { get; set; } = "";
        public bool Success { get; set; }
        public string? Error { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }

}
