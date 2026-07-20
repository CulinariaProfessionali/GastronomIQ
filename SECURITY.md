# Security Policy

## Reporting a Vulnerability

**Do not** open a public issue to report a security vulnerability. Instead, please report security vulnerabilities by emailing **security@gastronomiq.com** or through GitHub's [Security Advisories](https://github.com/CulinariaProfessionali/GastronomIQ/security/advisories).

### Reporting Process

1. **Email:** Send a detailed report to security@gastronomiq.com with:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact
   - Suggested fix (if any)

2. **GitHub Security Advisory:**
   - Visit: https://github.com/CulinariaProfessionali/GastronomIQ/security/advisories
   - Click "Report a vulnerability"
   - Provide detailed information

3. **Response Time:**
   - We aim to acknowledge reports within **48 hours**
   - We will work with you to understand and fix the issue
   - We will credit you in the security advisory (if desired)

### Security Support

| Version | Status | Support Until |
|---------|--------|---------------|
| 1.x (current) | Active | 2027-01-01 |
| 0.x | EOL | 2026-07-20 |

## Security Best Practices

### For Users

1. **Keep Dependencies Updated**
   - Run `npm audit` regularly
   - Update packages: `npm update`
   - Review breaking changes in major versions

2. **Manage Secrets Securely**
   - Never commit `.env` files
   - Use environment variables in production
   - Rotate API keys regularly
   - Use strong JWT secrets (minimum 32 characters)

3. **Authentication**
   - Require 2FA (Two-Factor Authentication)
   - Use strong, unique passwords
   - Store credentials in secure vaults

4. **Database Security**
   - Use strong PostgreSQL passwords
   - Enable SSL for database connections
   - Restrict database access to known IPs
   - Regular backups in secure locations

### For Developers

1. **Code Review**
   - All PRs must be reviewed before merge
   - Use branch protection rules
   - Require status checks (tests, linting)

2. **Commit Signing**
   - Sign commits with GPG keys
   - Verify commit signatures in CI/CD

3. **Dependency Management**
   ```bash
   npm audit           # Check for vulnerabilities
   npm audit fix       # Auto-fix low severity issues
   npm outdated        # Check for updates
   ```

4. **Environment Configuration**
   - Use `.env.example` for documentation
   - Never commit `.env` files
   - Use different secrets for dev/staging/prod

5. **Logging & Monitoring**
   - Log security-relevant events
   - Monitor for unauthorized access
   - Keep logs in secure locations
   - Implement rate limiting on APIs

## Security Testing

The following security tests are part of our CI/CD pipeline:

- ✅ **npm audit** - Dependency vulnerability scanning
- ✅ **ESLint** - Code quality and security rules
- ✅ **TypeScript strict mode** - Type safety
- ✅ **Helmet.js** - Security headers
- ✅ **CORS validation** - Cross-origin request protection
- ✅ **Input validation** - Zod schemas for request validation

## Infrastructure Security

### Database
- PostgreSQL with encryption at rest
- SSL/TLS for connections
- Parameterized queries to prevent SQL injection
- Regular backups

### Docker
- Run as non-root user
- Use minimal base images (Alpine)
- Regular image updates
- Signed container images (planned)

### API Security
- JWT authentication
- Rate limiting (planned)
- API key management
- CORS restrictions
- Helmet security headers

## Third-Party Integrations

### Authorized Services
- GitHub Actions (CI/CD)
- Dependabot (dependency updates)
- CodeQL (code scanning) - planned
- Codecov (coverage reporting) - planned

Review authorized integrations:
- GitHub Settings → Applications → Installed GitHub Apps
- GitHub Settings → Developer settings → Authorized OAuth Apps

## Security Incident Response

### If You Discover a Breach:

1. **Immediate Actions:**
   - Stop the compromise if possible
   - Document what happened
   - Collect evidence (logs, timestamps)

2. **Reporting:**
   - Contact: security@gastronomiq.com
   - Reference: [Incident Response Plan](./INCIDENT_RESPONSE.md) - planned

3. **Timeline:**
   - We commit to responding within 48 hours
   - Updates provided every 72 hours
   - Public disclosure coordinated with you

## Security Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/) - Common web vulnerabilities
- [Node.js Security Best Practices](https://nodejs.org/en/docs/guides/nodejs-web-security/) - Node.js security guide
- [PostgreSQL Security](https://www.postgresql.org/docs/current/sql-syntax.html) - Database security
- [Express.js Security](https://expressjs.com/en/advanced/best-practice-security.html) - Framework security

## Compliance

This project follows:
- [OWASP Secure Coding Practices](https://owasp.org/www-project-secure-coding-practices/stable-en/)
- [CWE Top 25](https://cwe.mitre.org/top25/) - Common weakness enumeration
- [GitHub Security Best Practices](https://docs.github.com/en/code-security)

## Questions?

For security questions or concerns:
- 📧 Email: security@gastronomiq.com
- 🐛 Report an issue: Use GitHub Security Advisories
- 📚 Documentation: See [docs/](./docs/)

---

**Last Updated:** 2026-07-20  
**Version:** 1.0  
**Maintainer:** CulinariaProfessionali
