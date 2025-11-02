#  Job Vacancy Collector

**Job Vacancy Collector** — a large-scale system for collecting, processing and storing vacancies from various sources (so far only from work.ua). The project is written in **.NET 8 / C#**

---

## Main features

- Parsing of vacancies from various sources (work.ua) across the country.
- Storing vacancies in the database (PostgreSQL).
- Deleting outdated vacancies.
- Exporting vacancies to **Excel** format.
- API for manually starting parsing and exporting.

---

## Note:

I wrote this project for people who might find it useful, so it is not used commercially, and because of this its support is not stable, but it is there. I also plan to write support for sites like Rabota.ua and Jobs.dou.ua. The first support was written for work.ua because this is the largest job search site in Ukraine. The quality of the code is not ideal and I may have broken some programming rules, so I will be glad if you suggest improving it.

---

## Architecture

The project has a multi-layered structure:


JobVacancyCollector:

├── Domain/ // Entities and business logic

├── Application/ // Use Cases and interfaces

├── Infrastructure/ // Implementation of repositories, DbContext, external services

└── WebAPI/ // API for data access and manual management

---

## Swagger:

<img width="1321" height="414" alt="image" src="https://github.com/user-attachments/assets/d29d1c05-90a4-45e8-8e22-a7e030cf4265" />
