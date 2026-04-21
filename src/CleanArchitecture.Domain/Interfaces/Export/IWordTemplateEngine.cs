using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Domain.Interfaces.Export
{
    public interface IWordTemplateEngine
    {
        /// <summary>
        /// Fill template Word (.docx) với data và trả về Stream
        /// </summary>
        /// <param name="templatePath">Đường dẫn file template .docx</param>
        /// <param name="data">Dictionary chứa các field và list động</param>
        /// <returns>Stream file .docx đã được fill data</returns>
        Stream FillTemplate(
            string templatePath,
            Dictionary<string, object?>? data);
    }
}
