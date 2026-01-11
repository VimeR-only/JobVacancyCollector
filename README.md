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

## Running with Docker

The easiest way to spin up the entire infrastructure (API, Worker, Database, RabbitMQ) is using **Docker Compose**.

### 1. Prerequisites
- **Docker** installed and running.

### 2. Launch the Environment
From the project root directory, run:
```bash
docker-compose up -d
```

### 3. Accessing the Services
| Service | URL / Port | Credentials (Default) |
| :--- | :--- | :--- |
| **Swagger UI (API)** | [http://localhost:8080/swagger](http://localhost:8080/swagger) | - |
| **pgAdmin** | [http://localhost:5050](http://localhost:5050) | `admin@admin.com` / `admin` |
| **RabbitMQ Management** | [http://localhost:15672](http://localhost:15672) | `guest` / `guest` |

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

<img width="1292" height="640" alt="image" src="https://github.com/user-attachments/assets/7a82ccea-fba6-4bbd-9b24-4cabf338fe70" />


---

## Contributing
Contributions are welcome! If you have suggestions for improving the code quality or adding new features, please feel free to open an issue or submit a pull request.
