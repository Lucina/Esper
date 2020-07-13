using System;
using System.Collections.Generic;
using Ns;

namespace Esper.Misaka
{
    /// <summary>
    /// NetSerializer types
    /// </summary>
    public static class NsMisaka
    {
        /// <summary>
        /// Converters
        /// </summary>
        public static readonly IReadOnlyDictionary<Type, (object encoder, object decoder)> Converters;

        static NsMisaka()
        {
            var converters = new Dictionary<Type, (object encoder, object decoder)>();

            #region BCL types

            #endregion

            #region Library types

            NetSerializer.RegisterCustom(converters, MisakaEntryIndex.Converter.encoder,
                MisakaEntryIndex.Converter.decoder);

            #endregion

            Converters = converters;
        }
    }
}
