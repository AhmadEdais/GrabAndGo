\# GRAB \& GO: WORKFLOW \& ROADMAP



\## The "Inside-Out" Development Workflow

When instructed to build a new feature, ALWAYS follow this order:

1\. Define the DTOs (Request/Response).

2\. Write the T-SQL Stored Procedure (using OPENJSON and FOR JSON PATH).

3\. Write the Repository interface and implementation (calling SqlExecutor).

4\. Write the Service interface and implementation (business logic).

5\. Write the API Controller endpoint.



\## Project Phases

\* \*\*Phase 0:\*\* Lookup Tables \& Products (Catalog)

\* \*\*Phase 1:\*\* Identity \& Auth (Registration, Login, JWT)

\* \*\*Phase 2:\*\* Session Management (QR Entry, Carts)

\* \*\*Phase 3:\*\* Vision Events (MQTT, Camera tracking, AI Labels)

\* \*\*Phase 4 \& 5:\*\* Financials (Checkout, Wallets, Receipts)

