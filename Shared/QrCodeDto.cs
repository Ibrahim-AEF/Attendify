using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class QrCodeDto
    {
        public string Code { get; set; }
        public string ImageUrl { get; set; }
        public DateTime ExpiryTime { get; set; }
    }
}
