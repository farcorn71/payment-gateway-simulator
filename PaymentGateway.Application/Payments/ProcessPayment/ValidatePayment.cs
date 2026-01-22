// var cardNumberResult = CardNumber.Create(request.CardNumber);
//             if (!cardNumberResult.IsSuccess)
//             {
//                 _logger.LogWarning("Payment {PaymentId} rejected: {Error}", paymentId, cardNumberResult.Error!.Message);
//                 return Result<ProcessPaymentResponse>.Failure(cardNumberResult.Error!);
//             }

//             var expiryDateResult = ExpiryDate.Create(request.ExpiryMonth, request.ExpiryYear);
//             if (!expiryDateResult.IsSuccess)
//             {
//                 _logger.LogWarning("Payment {PaymentId} rejected: {Error}", paymentId, expiryDateResult.Error!.Message);
//                 return Result<ProcessPaymentResponse>.Failure(expiryDateResult.Error!);
//             }

//             var currencyResult = Currency.Create(request.Currency);
//             if (!currencyResult.IsSuccess)
//             {
//                 _logger.LogWarning("Payment {PaymentId} rejected: {Error}", paymentId, currencyResult.Error!.Message);
//                 return Result<ProcessPaymentResponse>.Failure(currencyResult.Error!);
//             }

//             var moneyResult = Money.Create(request.Amount, currencyResult.Value!);
//             if (!moneyResult.IsSuccess)
//             {
//                 _logger.LogWarning("Payment {PaymentId} rejected: {Error}", paymentId, moneyResult.Error!.Message);
//                 return Result<ProcessPaymentResponse>.Failure(moneyResult.Error!);
//             }

//             var cvvResult = Cvv.Create(request.Cvv);
//             if (!cvvResult.IsSuccess)
//             {
//                 _logger.LogWarning("Payment {PaymentId} rejected: {Error}", paymentId, cvvResult.Error!.Message);
//                 return Result<ProcessPaymentResponse>.Failure(cvvResult.Error!);
//             }