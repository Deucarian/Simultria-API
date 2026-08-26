using System;

namespace Deucarian.Simultria.API.Editor
{
    [Serializable]
    internal sealed class SimultriaContractManifestDocument
    {
        public int schemaVersion;
        public SimultriaContractSource source;
        public SimultriaContractCatalogStatus catalog;
        public SimultriaContractCoverageStatus coverage;
    }

    [Serializable]
    internal sealed class SimultriaContractSource
    {
        public string fileName;
        public string openapiVersion;
        public string sha256;
        public string backendRevision;
    }

    [Serializable]
    internal sealed class SimultriaContractCatalogStatus
    {
        public string catalogId;
        public string displayName;
        public int operationCount;
        public int reviewedStableOperationCount;
        public int generatedOperationCount;
        public int unauthenticatedOperationCount;
    }

    [Serializable]
    internal sealed class SimultriaContractCoverageStatus
    {
        public int operationsInSuppliedSnapshot;
        public int catalogOperations;
        public string snapshotCoverage;
        public bool snapshotCoverageComplete;
        public int unusedOverlayKeyCount;
    }

    [Serializable]
    internal sealed class SimultriaContractChangeReport
    {
        public int schemaVersion;
        public SimultriaContractChangeSummary summary;
        public bool reviewRequired;
        public bool breakingOrSecurityReviewRequired;
        public SimultriaContractEndpointChange[] added;
        public SimultriaContractEndpointChange[] removed;
        public SimultriaContractChangedEndpoint[] changed;
    }

    [Serializable]
    internal sealed class SimultriaContractChangeSummary
    {
        public int added;
        public int removed;
        public int changed;
        public int total;
    }

    [Serializable]
    internal sealed class SimultriaContractEndpointChange
    {
        public string endpointId;
        public string method;
        public string routeTemplate;
        public string authentication;
        public bool suppressLogging;
    }

    [Serializable]
    internal sealed class SimultriaContractChangedEndpoint
    {
        public string endpointId;
        public string method;
        public string routeTemplate;
        public bool breakingOrSecurityReviewRequired;
        public SimultriaContractFieldChange[] changes;
    }

    [Serializable]
    internal sealed class SimultriaContractFieldChange
    {
        public string field;
    }

    internal sealed class SimultriaContractUpdateResult
    {
        internal bool Succeeded { get; set; }
        internal string Message { get; set; }
        internal string ProcessOutput { get; set; }
        internal string PreviewRoot { get; set; }
        internal SimultriaContractManifestDocument Manifest { get; set; }
        internal SimultriaContractChangeReport ChangeReport { get; set; }
    }
}
