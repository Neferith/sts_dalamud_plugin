using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sts.Domain.Content;

/// <summary>Métadonnées d'une image uploadée.</summary>
public sealed record ImageInfo(
    string FileName,
    string Url,
    int SizeKb);
