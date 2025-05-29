using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class ScanAttendanceDto
    {
        public string QRCodeData { get; set; }
        public bool FaceVerified { get; set; }
    }
}
