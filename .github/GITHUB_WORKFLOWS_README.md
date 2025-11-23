# GitHub Workflows Documentation

## 📋 Overview

This repository includes comprehensive GitHub Actions workflows for automated testing, code quality checks, and continuous integration.

## 🔄 Workflows

### 1. **CI/CD Pipeline** (`ci.yml`)

**Trigger**: Push to `main`/`develop`, Pull Requests  
**Purpose**: Main continuous integration pipeline

**Jobs**:
- ✅ **Build & Validate** - Compiles code and validates format
- ✅ **Unit Tests** - Runs isolated unit tests
- ✅ **Integration Tests** - Tests API endpoints with in-memory DB
- 🔒 **Security Tests** - Validates security headers and JWT
- 🔍 **Dependency Scan** - Checks for vulnerable packages
- 📊 **Code Quality** - Runs static code analysis
- ⚡ **Performance Check** - Runs performance tests
- 🗃️ **Migration Check** - Validates database migrations
- 🎯 **All Tests** - Combined test run with coverage

**Artifacts**:
- Build output
- Test results (TRX format)
- Code coverage reports
- Analysis results

**Duration**: ~5-10 minutes

---

### 2. **Test Coverage Report** (`test-coverage.yml`)

**Trigger**: Push to `main`/`develop`, PRs, Daily at 2 AM UTC  
**Purpose**: Generate detailed code coverage reports

**Features**:
- Generates HTML coverage reports
- Creates coverage badges
- Posts coverage summary on PRs
- Uploads to Codecov
- Tracks coverage trends

**Artifacts**:
- HTML coverage report
- Coverage badges
- Cobertura XML

**Duration**: ~3-5 minutes

---

### 3. **Nightly Tests** (`nightly-tests.yml`)

**Trigger**: Daily at 1 AM UTC, Manual  
**Purpose**: Comprehensive testing including long-running tests

**Jobs**:
- 🌐 **Comprehensive Tests** - All tests including explicit ones
- 🎯 **E2E Tests** - End-to-end user workflows
- 💪 **Stress Tests** - Load and stress testing
- ⚡ **Benchmark Tests** - Performance benchmarks
- 🧬 **Mutation Tests** - Mutation testing (when enabled)

**Features**:
- Creates GitHub issue on failure
- Sends notification summary
- Extended timeout (60 minutes)

**Duration**: ~30-60 minutes

---

### 4. **PR Checks** (`pr-checks.yml`)

**Trigger**: Pull Request opened/updated  
**Purpose**: Fast feedback for pull requests

**Jobs**:
- ✅ **PR Validation** - Quick validation and build
- 🧪 **Affected Tests** - Runs tests related to changes
- 📋 **Code Review Checks** - Format and static analysis
- 🏷️ **Auto Label** - Automatically labels PRs
- 📏 **Size Label** - Labels PR by size
- 💬 **Comment Summary** - Posts results to PR

**Features**:
- Fast feedback (<5 min)
- Lists changed files
- Coverage comparison
- Automated PR comments

**Duration**: ~3-5 minutes

---

## 🚀 Usage

### Running Workflows Manually

All workflows support manual triggering via `workflow_dispatch`:

```bash
# Using GitHub CLI
gh workflow run ci.yml
gh workflow run test-coverage.yml
gh workflow run nightly-tests.yml
```

Or via GitHub UI: **Actions** → **Select Workflow** → **Run workflow**

### Viewing Results

#### GitHub UI
1. Go to **Actions** tab
2. Select workflow run
3. View job details and artifacts

#### Test Reports
- Test results are displayed in the **Summary** page
- Download TRX files from artifacts for detailed analysis

#### Coverage Reports
- HTML reports available in artifacts
- View coverage trends in Codecov dashboard
- Badge URL: `https://codecov.io/gh/{owner}/{repo}/branch/main/graph/badge.svg`

---

## 📊 Status Badges

Add these badges to your README.md:

```markdown
![CI](https://github.com/{owner}/{repo}/workflows/CI%2FCD%20Pipeline/badge.svg)
![Coverage](https://codecov.io/gh/{owner}/{repo}/branch/main/graph/badge.svg)
![Tests](https://github.com/{owner}/{repo}/workflows/Nightly%20Tests/badge.svg)
```

---

## 🔧 Configuration

### Secrets Required

None required for basic functionality. Optional:
- `CODECOV_TOKEN` - For private repos (codecov.io)
- Custom notification tokens (Slack, Teams, etc.)

### Environment Variables

Configured in workflows:
- `DOTNET_VERSION: '9.0.x'` - .NET SDK version
- `DOTNET_SKIP_FIRST_TIME_EXPERIENCE: 1`
- `DOTNET_NOLOGO: true`

### Modifying Workflows

#### Change .NET Version
```yaml
env:
  DOTNET_VERSION: '9.0.x'  # Update this
```

#### Change Test Filter
```bash
dotnet test --filter "FullyQualifiedName~UnitTests"  # Modify filter
```

#### Add New Job
```yaml
new-job:
  name: My New Job
  runs-on: ubuntu-latest
  steps:
    - name: Checkout
      uses: actions/checkout@v4
    # Add your steps
```

---

## 🎯 Test Categories

Tests are organized by filters:

| Filter | Description | When Run |
|--------|-------------|----------|
| `FullyQualifiedName~UnitTests` | Unit tests only | Every PR/Push |
| `FullyQualifiedName~IntegrationTests` | Integration tests | Every PR/Push |
| `FullyQualifiedName~E2ETests` | End-to-end tests | Nightly |
| `FullyQualifiedName~SecurityTests` | Security tests | CI Pipeline |
| `TestCategory=Load` | Load tests | Performance check |
| `TestCategory=Stress` | Stress tests | Nightly |

---

## 📈 Performance Optimization

### Caching
The workflows automatically cache:
- NuGet packages
- .NET tools
- Build outputs

### Parallel Execution
Jobs run in parallel when possible:
- Unit and Integration tests run concurrently
- Security and quality checks run in parallel

### Artifacts Retention
- Test results: 30 days
- Build artifacts: 1 day
- Coverage reports: 30 days

---

## 🐛 Troubleshooting

### Tests Fail in CI but Pass Locally

**Possible causes**:
1. Environment differences
2. Timezone issues
3. File path differences (Windows vs Linux)
4. Database state

**Solutions**:
1. Run tests in container locally
2. Check timezone handling
3. Use `Path.Combine()` for paths
4. Ensure tests are isolated

### Workflow Fails to Start

**Possible causes**:
1. YAML syntax error
2. Missing required files
3. Permission issues

**Solutions**:
1. Validate YAML: `yamllint .github/workflows/*.yml`
2. Check file paths in workflow
3. Review repository settings → Actions

### Coverage Upload Fails

**Solutions**:
1. Check if coverage files exist: `ls -R **/coverage*.xml`
2. Verify Codecov token (for private repos)
3. Check Codecov service status

---

## 📚 Best Practices

1. **Keep workflows focused** - Separate concerns (CI, coverage, nightly)
2. **Use matrix builds** - Test multiple versions when needed
3. **Cache dependencies** - Speed up workflows
4. **Fail fast** - Put quick checks first
5. **Use artifacts** - Share data between jobs
6. **Add summaries** - Use `$GITHUB_STEP_SUMMARY`
7. **Comment on PRs** - Provide feedback automatically
8. **Monitor performance** - Track workflow duration

---

## 🔄 Workflow Dependencies

```
ci.yml (Main Pipeline)
├─ Build & Validate
├─ Unit Tests (depends: build)
├─ Integration Tests (depends: build)
├─ Security Tests (depends: build)
└─ All Tests (depends: unit, integration)

test-coverage.yml (Coverage)
└─ Standalone, runs all tests with coverage

nightly-tests.yml (Comprehensive)
├─ Comprehensive Tests
├─ E2E Tests
├─ Stress Tests
└─ Benchmark Tests

pr-checks.yml (Quick Feedback)
├─ PR Validation
├─ Affected Tests
├─ Code Review Checks
└─ Comment Summary (depends: validation, tests)
```

---

## 📝 Maintenance

### Regular Updates

1. **Update actions versions** - Check for updates monthly
   ```yaml
   uses: actions/checkout@v4  # Check for v5, v6, etc.
   ```

2. **Update .NET version** - When new versions release
   ```yaml
   DOTNET_VERSION: '9.0.x'  # Update to 10.0.x when available
   ```

3. **Review and update filters** - As tests evolve
4. **Check for deprecated features** - GitHub announces deprecations
5. **Monitor workflow performance** - Optimize slow steps

### Adding New Tests

When adding new test types:

1. Update filters in workflows
2. Add appropriate job if needed
3. Update this documentation
4. Consider adding to nightly vs. CI

---

## 💡 Tips

- Use `act` tool to test workflows locally: `act -j build`
- View workflow syntax: `gh workflow view ci.yml`
- Debug with `tmate`: Add `uses: mxschmitt/action-tmate@v3`
- Check workflow runs: `gh run list`
- Download artifacts: `gh run download <run-id>`

---

## 🤝 Contributing

When modifying workflows:

1. Test locally with `act` if possible
2. Create PR with workflow changes
3. Watch first run carefully
4. Update this documentation
5. Add comments in workflow YAML

---

## 📞 Support

For issues with workflows:
1. Check workflow run logs
2. Review this documentation
3. Check GitHub Actions documentation
4. Create an issue with workflow run link

---

**Last Updated**: 2025-11-23  
**Workflows Version**: 1.0.0  
**Maintainer**: Development Team
