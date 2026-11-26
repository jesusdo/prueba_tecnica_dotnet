using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Transactions;

namespace PropertyMaintenance.Api.Exceptions;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger, IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var known = exception as RequestException;
        var sql = exception as SqlException ?? exception.InnerException as SqlException;
        var conflict = exception is DbUpdateConcurrencyException or TransactionAbortedException
            || sql?.Number is 1205 or 1222;
        var status = known?.Status ?? (conflict ? 409 : 500);
        if (status == 500) logger.LogError(exception, "Unexpected error creating maintenance request.");
        var detail = new ProblemDetails
        {
            Status = status,
            Title = known?.Message ?? (conflict ? "A concurrent change prevented this operation. Retry the request." : "An unexpected error occurred."),
            Instance = context.Request.Path
        };
        detail.Extensions["code"] = known?.Code ?? (exception is DbUpdateConcurrencyException
            ? "CONCURRENCY_CONFLICT" : conflict ? "CONCURRENT_CHANGE" : "UNEXPECTED_ERROR");
        if (conflict) detail.Extensions["conflictType"] = exception is DbUpdateConcurrencyException ? "ROW_VERSION"
            : sql?.Number == 1205 ? "DEADLOCK" : sql?.Number == 1222 ? "LOCK_TIMEOUT" : "TRANSACTION_ABORTED";
        context.Response.StatusCode = status;
        await problems.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = detail });
        return true;
    }
}
