using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using Shared.Entities;
using Shared.Models;

using Shared.Constants;

namespace Shared.Services
{
    public class ReturnProcessingService : IReturnProcessingService
    {
        private readonly IOAuthClientService _oAuthClientService;
        private readonly IOrderService _orderService;
        private readonly IConsignmentService _consignmentService;
        private readonly IReturnService _returnService;
        private readonly IEmailService _emailService;
        private readonly IValidator<Return> _returnValidator;
        private readonly ILogger<ReturnProcessingService> _logger;

        public ReturnProcessingService(IOAuthClientService oAuthClientService,
                                       IOrderService orderService,
                                       IConsignmentService consignmentService,
                                       IReturnService returnService,
                                       IEmailService emailService,
                                       IValidator<Return> returnValidator,
                                       ILogger<ReturnProcessingService> logger)
        {
            _oAuthClientService = oAuthClientService;
            _orderService = orderService;
            _consignmentService = consignmentService;
            _returnService = returnService;
            _emailService = emailService;
            _returnValidator = returnValidator;
            _logger = logger;
        }

        public async Task ReadAndSaveReturnsAsync()
        {
            try
            {
                var returns = await _oAuthClientService.GetApiReturnsWithStatusCreatedAsync(SharedStatus.Created);

                if (returns.ReturnRequests.Count > 0)
                {
                    await _emailService.SendReturnNotificationEmailAsync();
                }

                var parsedReturnsList = _returnService.ParseReturnResponseToReturnObject(returns);

                foreach (var parsedReturn in parsedReturnsList)
                {
                    await ProcessSingleReturnAsync(parsedReturn);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ein Fehler ist beim Abrufen und Erzeugen der Returns aufgetreten.");
            }
        }

        private async Task ProcessSingleReturnAsync(Return parsedReturn)
        {
            try
            {
                await _returnValidator.ValidateAndThrowAsync(parsedReturn);

                foreach (var returnEntry in parsedReturn.ReturnEntries)
                {
                    _logger.LogInformation($"Für die Retoure mit dem EntryCode '{returnEntry.EntryCode}' wurde folgende Menge abgerufen: {returnEntry.Quantity}");
                }

                var returnInProgress = _returnService.ParseReturnToReturnInProgressRequest(parsedReturn);
                if (returnInProgress == null)
                {
                    return;
                }

                var createReturnResult = await _returnService.CreateReturnAsync(parsedReturn);
                if (createReturnResult)
                {
                    await UpdateConsignmentEntries(parsedReturn);
                    await NotifyApiOfReturnInProgress(returnInProgress);
                }
                else
                {
                    _logger.LogError($"Fehler beim Erstellen des Returns in der Datenbank. Rma code: '{parsedReturn.Rma}', OrderCode: '{parsedReturn.OrderCode}', AldiReturnCode: '{parsedReturn.AldiReturnCode}'.");
                }
            }
            catch (ValidationException ex)
            {
                _logger.LogError(ex, $"Validierungsfehler für Retoure mit OrderCode '{parsedReturn.OrderCode}' und AldiReturnCode '{parsedReturn.AldiReturnCode}': {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Bearbeiten des Returns.");
            }
        }

        private async Task UpdateConsignmentEntries(Return parsedReturn)
        {
            foreach (var returnEntry in parsedReturn.ReturnEntries)
            {
                var order = await _orderService.GetOrderByOrderCodeAsync(parsedReturn.OrderCode);
                await _consignmentService.UpdateConsignmentEntryQuantitiesAsync(order, returnEntry);
            }
        }

        private async Task NotifyApiOfReturnInProgress(ReturnInProgressRequest returnInProgress)
        {
            try
            {
                await _oAuthClientService.ReturnInProgress(returnInProgress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Benachrichtigen der API über den INPROGRESS-Status des Returns.");
                throw;
            }
        }
    }
}
