#  Job Vacancy Collector

**Job Vacancy Collector** — a high-performance system designed for large-scale collection, processing, and storage of job vacancies from various Ukrainian job boards. 

The project is built with **.NET 8** and leverages modern asynchronous patterns to ensure scalability and efficiency.

---

## Main features

* **Multi-Source Scrapping:** Support for **Work.ua** and **Jobs.dou.ua** with an extensible architecture for adding new providers (e.g., Rabota.ua, Jooble).
* **Parallel Execution:** Run multiple scrapers simultaneously using isolated `IServiceScope` to ensure database integrity and high throughput.
* **Memory Efficiency:** Built on `IAsyncEnumerable` (Async Streams) to process thousands of vacancies without high RAM consumption.
* **Smart Storage:** Batch saving to **PostgreSQL** to minimize database roundtrips.
- **Data Maintenance:** Automatic cleanup of outdated vacancies.
- **Reporting:** Export collected data directly to **Excel** format.
- **Developer Friendly:** Fully documented API via **Swagger UI**.

---

## Architecture & Technologies

The system follows a clean, multi-layered architecture:

-   **Domain:** Core entities and repository abstractions.
-   **Application:** Business logic, Scraper Factory (Strategy Pattern), and Service interfaces.
-   **Infrastructure:** EF Core implementations, PostgreSQL integration, and HTML parsers using AngleSharp/HttpClient.
-   **WebAPI:** RESTful controllers for manual management and data access.

### Tech Stack
-   **.NET 8 (C#)**
-   **Entity Framework Core**
-   **PostgreSQL**
-   **HttpClient Factory**
-   **Swagger / OpenAPI**

---

## Getting Started

### Prerequisites
- .NET 8 SDK
- PostgreSQL instance

### Installation
1.  **Clone the repository:**
    ```bash
    git clone https://github.com/VimeR-only/JobVacancyCollector.git
    ```
2.  **Configure Database:**
    Update the connection string in `appsettings.json`:
    ```json
    "ConnectionStrings": {
      "WebApiDb": "Host=localhost;Database=JobVacancyDb;Username=postgres;Password=your_password"
    }
    ```
3.  **Apply Migrations:**
    ```bash
    dotnet ef database update
    ```
4.  **Run the application:**
    ```bash
    dotnet run --project JobVacancyCollector
    ```

---

## Roadmap
- [x] Parallel scraping support.
- [x] Integration with Work.ua and Jobs.dou.ua.
- [ ] Integration with Rabota.ua and Jooble.
- [ ] Advanced cross-source deduplication logic.
- [ ] Telegram Bot for real-time vacancy alerts.
- [ ] Frontend dashboard (React/Blazor).

---

API Documentation (Swagger)

The API allows you to trigger scrapers manually and monitor the data flow.

<img width="1313" height="607" alt="image" src="https://github.com/user-attachments/assets/58a2342b-aca2-4a76-b7cf-d1941070bbf0" />

---

## Contributing
Contributions are welcome! If you have suggestions for improving the code quality or adding new features, please feel free to open an issue or submit a pull request.
