# План переноса UploadFilesUpdateReferences_2 в чистую архитектуру

## Задача
Перенести метод `UploadFilesUpdateReferences_2` из master в develop по принципу чистой архитектуры (Clean Architecture).

## Что нужно сделать

### 1. Расширить IMinioService (Application/Interfaces)
Добавить методы:
- `Task<string> GetObjectETagAsync(string bucketName, string objectName)`
- `Task<Stream> GetFileAsync(string bucketName, string objectName)`
- `Task UploadFileAsync2(string bucketName, string fileName, Stream dataStream, long length)`
- `Task<UploadResultDTO> UploadFilesAsync(string bucketName, List<IFormFile> filesData)`
- `Task<FileReferenceDTO> AddReferenceAsync(string userId, string fileName, string fileExtension, string fileReferenceMinio)`
- `Task<List<FileReferenceDTO>> GetReferencesByUserIdAsync(string userId)`

### 2. Расширить MinioService (Application/Services)
Реализовать все интерфейсные методы с логикой работы с MinIO и Excel/SPC файлами.

### 3. Расширить DTO (Shared/Dtos)
- `UploadFilesRequestDTO` уже есть
- Проверить FileReferenceDTO (уже есть)
- Добавить `GetReferencesByUserIdResponseDTO` если нужно

### 4. Добавить метод в MinioController (WebAPI/Controllers)
Реализовать `UploadFilesUpdateReferences_2` в контроллере с валидацией и обработкой ошибок.

### 5. Обновить Context (Infrastructure/Database)
Убедиться, что `UserReferencesInMinio` dbset доступен.

## Примечания
- В master используется `JWTAuthTemplate.Extensions.MinioService` с полной реализацией
- В develop используется `JWTAuthTemplate.Application.Services.MinioService` с базовой реализацией
- Нужно сохранить функциональность проверки уникальности (FileName + FileExtension)
- Обработка ошибок должна продолжать обработку других файлов при ошибке одного
