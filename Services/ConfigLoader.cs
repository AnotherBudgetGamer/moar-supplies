using Microsoft.Extensions.Logging;
using MoarSupplies.Models;
using MoarSupplies.Validation;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace MoarSupplies.Services;

/// <summary>
/// Loads the user-facing configuration after SPT has constructed its dependency-injection container.
/// </summary>
[Injectable(InjectionType.Singleton, 0)]
public sealed class ConfigLoader : IOnLoad
{
    private readonly ILogger<ConfigLoader> _logger;
    private readonly ConfigValidator _configValidator;
    private readonly DebugSettings _debugSettings;
    private readonly ConfigState _configState;
    private readonly ConfigStorage _configStorage;
    private readonly StimService _stimService;

    public ConfigLoader(
        ILogger<ConfigLoader> logger,
        ConfigValidator configValidator,
        DebugSettings debugSettings,
        ConfigState configState,
        ConfigStorage configStorage,
        StimService stimService)
    {
        _logger = logger;
        _configValidator = configValidator;
        _debugSettings = debugSettings;
        _configState = configState;
        _configStorage = configStorage;
        _stimService = stimService;
    }

    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ConfigLoadResult loadResult;
        try
        {
            loadResult = await _configStorage.LoadAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or InvalidDataException)
        {
            _logger.LogError("[MoarSupplies] Configuration could not be loaded: {Message}", exception.Message);
            return;
        }

        ModConfig config = loadResult.Config;
        _debugSettings.Enabled = config.Debug;
        if (_debugSettings.Enabled)
        {
            _logger.LogInformation("[MoarSupplies] Loading configuration from {ConfigPath}.{LegacySuffix}", loadResult.SourcePath, loadResult.UsesLegacyFormat ? " Legacy aggregate format will be migrated when you save a stim." : string.Empty);
            _logger.LogInformation("[MoarSupplies] Verbose debug logging is enabled.");
        }

        IReadOnlyList<string> validationErrors = _configValidator.Validate(config);
        if (validationErrors.Count > 0)
        {
            _logger.LogError("[MoarSupplies] Error loading {ErrorCount} stim definition validation error(s). No stims will be registered.", validationErrors.Count);

            if (_debugSettings.Enabled)
            {
                foreach (string error in validationErrors)
                {
                    _logger.LogError("[MoarSupplies] {ValidationError}", error);
                }
            }

            return;
        }

        if (_debugSettings.Enabled)
        {
            _logger.LogInformation("[MoarSupplies] Configuration validation passed.");
        }

        _configState.Current = config;
        _logger.LogInformation("[MoarSupplies] Configuration loaded.");
        _logger.LogInformation("[MoarSupplies] Loaded {StimCount} stim definition(s).", config.Stims.Count);

        foreach (StimDefinition stim in config.Stims)
        {
            if (_debugSettings.Enabled)
            {
                _logger.LogInformation("[MoarSupplies] Processing stim '{StimId}': {StimName}.", stim.Id, stim.Identity.Name);
            }

            if (!stim.Enabled)
            {
                if (_debugSettings.Enabled)
                {
                    _logger.LogInformation("[MoarSupplies] Stim '{StimId}' is disabled; registration was skipped.", stim.Id);
                }

                continue;
            }

            if (!_stimService.Register(stim))
            {
                _logger.LogError("[MoarSupplies] Registration stopped after stim '{StimId}' failed.", stim.Id);
                return;
            }

            if (_debugSettings.Enabled)
            {
                _logger.LogInformation("[MoarSupplies] Registered stim '{StimId}' with {BuffCount} buff(s).", stim.Id, stim.Buffs.Count);
            }
        }
    }

}
