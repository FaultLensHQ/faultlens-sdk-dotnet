namespace FaultLens.Sdk.Transport
{
    /// <summary>Internal mirror of <see cref="FaultLens.Sdk.DeliveryFailureKind"/>, kept separate so the
    /// transport layer can evolve independently of the public result type.</summary>
    internal enum IngestFailureKind
    {
        Unknown = 0,
        IdentityConflict,
        CapacityExhausted,
        ServiceUnavailable,
        Throttled,
        NetworkError,
        Http
    }
}
