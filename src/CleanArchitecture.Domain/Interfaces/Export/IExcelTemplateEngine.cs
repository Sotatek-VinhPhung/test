using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Domain.Interfaces.Export
{
    public interface IExcelTemplateEngine
    {
        Stream FillTemplate(
            string templatePath,
            Dictionary<string, object?>? data,
            string? sheetName = null);
    }
}
