using System;
using System.Collections.Generic;
using Lodino.Core.Api;
using Lodino.Core.Errors;
using Lodino.Core.Formats.Archive.Gzip;
using Lodino.Core.Formats.Archive.Zip;
using Lodino.Core.Formats.Audio.Wav;
using Lodino.Core.Formats.Data;
using Lodino.Core.Formats.Image;
using Lodino.Core.Formats.Image.Jpeg;
using Lodino.Core.Formats.Image.Png;

namespace Lodino.Core.Registry
{
    public class EngineRegistry
    {
        private static readonly Lazy<EngineRegistry> _default = new Lazy<EngineRegistry>(() =>
        {
            var registry = new EngineRegistry();
            registry.Register(new StructuredDataEngine());
            registry.Register(new BmpEngine());
            registry.Register(new PngEngine());
            registry.Register(new JpegEngine());
            registry.Register(new WavEngine());
            registry.Register(new ZipEngine());
            registry.Register(new GzipEngine());
            return registry;
        });

        public static EngineRegistry Default
        {
            get { return _default.Value; }
        }

        private readonly List<ICoreEngine> _engines = new List<ICoreEngine>();

        public void Register(ICoreEngine engine)
        {
            if (engine == null) throw new ArgumentNullException("engine");
            _engines.Add(engine);
        }

        public ICoreEngine Resolve(string sourceFormat, string targetFormat)
        {
            for (int i = 0; i < _engines.Count; i++)
            {
                if (_engines[i].CanConvert(sourceFormat, targetFormat))
                {
                    return _engines[i];
                }
            }

            throw new UnsupportedFormatException(string.Format(
                "Nenhum motor próprio registrado suporta a rota de conversão: .{0} -> .{1}",
                sourceFormat, targetFormat));
        }

        public IEnumerable<ICoreEngine> GetAll()
        {
            return _engines.AsReadOnly();
        }
    }
}
