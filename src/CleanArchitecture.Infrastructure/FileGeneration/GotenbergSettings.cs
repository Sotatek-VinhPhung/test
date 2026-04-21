using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.FileGeneration
{
    public class GotenbergSettings
    {
        public const string SectionName = "Gotenberg";
        public string BaseUrl { get; set; } = "http://gotenberg:3000";
        public int TimeoutSeconds { get; set; } = 120;
    }
}
