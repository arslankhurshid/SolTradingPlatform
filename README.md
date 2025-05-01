#  Credit Payment Gateway with gRPC Logging

This solution demonstrates a simple **Credit Payment Gateway** system that logs payment errors via a **gRPC Logging Service**.

---

##  Projects Overview

| Project | Description |
|--------|-------------|
| `CreditPaymentGateway` | ASP.NET Core Web API that accepts credit card payments and logs errors to a remote gRPC service |
| `GrpcLoggingService`  | A simple gRPC server that receives and stores log messages |

---

## 🛠 Technologies Used

- .NET 8 (or 7)
- ASP.NET Core Web API
- gRPC (via `Grpc.Net.Client`)
- Protobuf for message definitions
- Swagger (for testing API)
- Visual Studio / Rider

---

##  Running the Solution

### 1. Start gRPC Logging Service

```bash
cd GrpcLoggingService
dotnet run

### Test via Postman
POST http://localhost:5241/CreditPayment/pay
JSON Body
{
  "cardNumber": "4111111111111111",
  "cardHolder": "John Doe",
  "expiration": "12/25",
  "amount": 99.99
}
