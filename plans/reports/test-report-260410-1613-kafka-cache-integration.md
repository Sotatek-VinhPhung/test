# Test Report — 2026-04-10 — Kafka + Cache Integration

## Test Results Overview
- **Total**: 14 tests
- **Passed**: 14 | **Failed**: 0 | **Skipped**: 0
- **Duration**: ~17s total

### Breakdown by Project

| Project | Tests | Passed | Failed | Duration |
|---------|-------|--------|--------|----------|
| CleanArchitecture.Domain.Tests | 3 | 3 | 0 | 902ms |
| CleanArchitecture.Application.Tests | 6 | 6 | 0 | 2.5s |
| CleanArchitecture.Api.Tests | 5 | 5 | 0 | 5.6s |

### Individual Test Results

**Domain Tests (3/3 PASS)**
- `NotFoundExceptionTests.NotFoundException_ShouldContain_EntityNameAndKey` — 24ms
- `UserEntityTests.User_ShouldSetProperties_Correctly` — 31ms
- `UserEntityTests.User_ShouldInitialize_WithDefaultValues` — <1ms

**Application Tests (6/6 PASS)**
- `UserMappingsTests.ToDto_ShouldMapCorrectly` — 39ms
- `UserServiceTests.UpdateAsync_ShouldUpdateAndSave` — 275ms
- `UserServiceTests.GetByIdAsync_WhenUserExists_ShouldReturnUser` — 4ms
- `UserServiceTests.GetAllAsync_ShouldReturnAllUsers` — 6ms
- `UserServiceTests.GetByIdAsync_WhenUserNotFound_ShouldThrowNotFoundException` — 15ms
- `UserServiceTests.DeleteAsync_ShouldDeleteAndSave` — 6ms

**Api Integration Tests (5/5 PASS)**
- `AuthControllerTests.Register_WithValidData_ShouldReturn201` — 3s
- `AuthControllerTests.Login_WithInvalidPassword_ShouldReturn400` — 452ms
- `AuthControllerTests.Users_WithoutAuth_ShouldReturn401` — 14ms
- `AuthControllerTests.Register_DuplicateEmail_ShouldReturn400` — 193ms
- `AuthControllerTests.Login_WithValidCredentials_ShouldReturn200` — 400ms

## Build Status
- **Build**: PASS (0 errors)
- **Warnings**: 2 pre-existing (EF Core Relational version conflict — MSB3277, unrelated to Kafka/Cache changes)
- **Dependencies**: All resolved (Confluent.Kafka 2.6.1, StackExchangeRedis 8.0.*)

## Coverage Metrics
- Coverage tooling not configured in this project
- No `coverlet` or equivalent NuGet present

## Failed Tests
None.

## Critical Issues
None — zero regressions from Kafka integration + Cache abstraction.

## Recommendations
1. **[Medium]** Add unit tests for Kafka and Cache infrastructure services (mock IMemoryCache, IDistributedCache, IProducer)
2. **[Low]** Add `coverlet.collector` NuGet to test projects for coverage reporting
3. **[Low]** Pin EF Core versions to resolve MSB3277 warnings
