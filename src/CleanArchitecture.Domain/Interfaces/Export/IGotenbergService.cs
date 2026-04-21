using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Domain.Interfaces.Export
{
    public interface IGotenbergService
    {
        Task<Stream> ConvertOfficeToPdfAsync(
        Stream officeFileStream,
        string fileName,
        CancellationToken cancellationToken = default);
    }
}
