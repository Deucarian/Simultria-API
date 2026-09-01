using System;
using System.Collections.Generic;
using Deucarian.API.Models;

namespace Deucarian.Simultria.API.Configuration
{
    /// <summary>
    /// Canonical, credential-free Simultria deployment stages. Descriptors
    /// identify known environments without implying or storing a host.
    /// </summary>
    public static class SimultriaEnvironmentDescriptors
    {
        public static readonly ApiEnvironmentDescriptor Local =
            new ApiEnvironmentDescriptor(
                SimultriaEnvironmentIds.Local,
                ApiEnvironmentStage.Custom,
                "Local");

        public static readonly ApiEnvironmentDescriptor Development =
            new ApiEnvironmentDescriptor(
                SimultriaEnvironmentIds.Development,
                ApiEnvironmentStage.Development,
                "Development");

        public static readonly ApiEnvironmentDescriptor Testing =
            new ApiEnvironmentDescriptor(
                SimultriaEnvironmentIds.Testing,
                ApiEnvironmentStage.Testing,
                "Testing");

        public static readonly ApiEnvironmentDescriptor Acceptance =
            new ApiEnvironmentDescriptor(
                SimultriaEnvironmentIds.Acceptance,
                ApiEnvironmentStage.Acceptance,
                "Acceptance");

        public static readonly ApiEnvironmentDescriptor Production =
            new ApiEnvironmentDescriptor(
                SimultriaEnvironmentIds.Production,
                ApiEnvironmentStage.Production,
                "Production");

        private static readonly IReadOnlyList<ApiEnvironmentDescriptor>
            standard = Array.AsReadOnly(new[]
            {
                Development,
                Testing,
                Acceptance,
                Production
            });

        private static readonly IReadOnlyList<ApiEnvironmentDescriptor>
            all = Array.AsReadOnly(new[]
            {
                Local,
                Development,
                Testing,
                Acceptance,
                Production
            });

        /// <summary>
        /// Development, Testing, Acceptance, and Production in canonical
        /// presentation order.
        /// </summary>
        public static IReadOnlyList<ApiEnvironmentDescriptor> Standard =>
            standard;

        /// <summary>
        /// Local plus the four conventional deployment stages in selectable
        /// presentation order.
        /// </summary>
        public static IReadOnlyList<ApiEnvironmentDescriptor> All => all;
    }
}
