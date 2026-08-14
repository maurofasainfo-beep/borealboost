namespace BorealBoost.Core.Optimization;

public sealed record TrustedRegistryOperationTarget(
    OptimizationId OptimizationId,
    OperationId OperationId,
    OperationType OperationType,
    RegistryOperationTarget Target,
    RegistryValueState DesiredState);

public static class TrustedRegistryOperationTargets
{
    public static readonly IReadOnlyList<TrustedRegistryOperationTarget> CatalogV1 =
    [
        DWord(
            "BB.OPT.VISUAL.TRANSPARENCY.DISABLE",
            "BB.OP.VISUAL.TRANSPARENCY.DISABLE",
            RegistryHiveKind.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "EnableTransparency",
            0),
        DWord(
            "BB.OPT.WINDOWS.EXPLORER.SHOW_EXTENSIONS",
            "BB.OP.WINDOWS.EXPLORER.SHOW_EXTENSIONS",
            RegistryHiveKind.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            "HideFileExt",
            0),
        DWord(
            "BB.OPT.WINDOWS.AUTOPLAY.DISABLE",
            "BB.OP.WINDOWS.AUTOPLAY.DISABLE",
            RegistryHiveKind.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers",
            "DisableAutoplay",
            1),
        DWord(
            "BB.OPT.PRIVACY.START.RECOMMENDATIONS.DISABLE",
            "BB.OP.PRIVACY.START.RECOMMENDATIONS.DISABLE",
            RegistryHiveKind.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            "Start_IrisRecommendations",
            0),
        DWord(
            "BB.OPT.WINDOWS.START.MORE_PINS",
            "BB.OP.WINDOWS.START.MORE_PINS",
            RegistryHiveKind.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            "Start_Layout",
            1),
        DWord(
            "BB.OPT.GAMING.GAMEBAR.CONTROLLER_SHORTCUT.DISABLE",
            "BB.OP.GAMING.GAMEBAR.CONTROLLER_SHORTCUT.DISABLE",
            RegistryHiveKind.CurrentUser,
            @"Software\Microsoft\GameBar",
            "UseNexusForGameBarEnabled",
            0),
        DWord(
            "BB.OPT.GAMING.GAMEBAR.RECORDING_SHORTCUT.DISABLE",
            "BB.OP.GAMING.GAMEBAR.RECORDING_SHORTCUT.DISABLE",
            RegistryHiveKind.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\GameDVR",
            "VKMToggleRecording",
            0),
        DWord(
            "BB.OPT.GAMING.GAMEBAR.BROADCAST_SHORTCUT.DISABLE",
            "BB.OP.GAMING.GAMEBAR.BROADCAST_SHORTCUT.DISABLE",
            RegistryHiveKind.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\GameDVR",
            "VKMToggleBroadcast",
            0),
        DWord(
            "BB.OPT.PRIVACY.GAMEBAR.CAMERA_CAPTURE_SHORTCUT.DISABLE",
            "BB.OP.PRIVACY.GAMEBAR.CAMERA_CAPTURE_SHORTCUT.DISABLE",
            RegistryHiveKind.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\GameDVR",
            "VKMToggleCameraCapture",
            0),
        DWord(
            "BB.OPT.PRIVACY.GAMEBAR.MIC_CAPTURE_SHORTCUT.DISABLE",
            "BB.OP.PRIVACY.GAMEBAR.MIC_CAPTURE_SHORTCUT.DISABLE",
            RegistryHiveKind.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\GameDVR",
            "VKMToggleMicrophoneCapture",
            0),
        DWord(
            "BB.OPT.PRIVACY.ADVERTISING_ID.DISABLE",
            "BB.OP.PRIVACY.ADVERTISING_ID.DISABLE",
            RegistryHiveKind.LocalMachine,
            @"Software\Policies\Microsoft\Windows\AdvertisingInfo",
            "DisabledByGroupPolicy",
            1),
        DWord(
            "BB.OPT.GAMING.GAME_DVR_POLICY.DISABLE",
            "BB.OP.GAMING.GAME_DVR_POLICY.DISABLE",
            RegistryHiveKind.LocalMachine,
            @"Software\Policies\Microsoft\Windows\GameDVR",
            "AllowGameDVR",
            0)
    ];

    public static bool TryFind(OperationSpec operation, out TrustedRegistryOperationTarget target)
    {
        target = CatalogV1.FirstOrDefault(candidate => Matches(candidate, operation))!;
        return target is not null;
    }

    public static bool IsTrustedCatalogOperation(OperationSpec operation)
    {
        return TryFind(operation, out _);
    }

    private static TrustedRegistryOperationTarget DWord(
        string optimizationId,
        string operationId,
        RegistryHiveKind hive,
        string keyPath,
        string valueName,
        int value)
    {
        return new TrustedRegistryOperationTarget(
            new OptimizationId(optimizationId),
            new OperationId(operationId),
            OperationType.RegistryValue,
            new RegistryOperationTarget(hive, keyPath, valueName, RegistryViewKind.Default),
            new RegistryValueState(true, RegistryValueDataKind.DWord, null, value));
    }

    private static bool Matches(TrustedRegistryOperationTarget candidate, OperationSpec operation)
    {
        return operation.OperationId == candidate.OperationId &&
               operation.OperationType == candidate.OperationType &&
               operation.RegistryValue is not null &&
               operation.RegistryValue.Target == candidate.Target &&
               RegistryStateEquals(operation.RegistryValue.DesiredState, candidate.DesiredState);
    }

    private static bool RegistryStateEquals(RegistryValueState first, RegistryValueState second)
    {
        return first.Exists == second.Exists &&
               first.ValueKind == second.ValueKind &&
               string.Equals(first.StringValue, second.StringValue, StringComparison.Ordinal) &&
               first.DWordValue == second.DWordValue &&
               first.QWordValue == second.QWordValue &&
               SequenceEqual(first.MultiStringValue, second.MultiStringValue) &&
               BinaryEqual(first.BinaryValue, second.BinaryValue);
    }

    private static bool SequenceEqual(IReadOnlyList<string>? first, IReadOnlyList<string>? second)
    {
        if (first is null || second is null)
        {
            return first is null && second is null;
        }

        return first.SequenceEqual(second, StringComparer.Ordinal);
    }

    private static bool BinaryEqual(byte[]? first, byte[]? second)
    {
        if (first is null || second is null)
        {
            return first is null && second is null;
        }

        return first.AsSpan().SequenceEqual(second);
    }
}
