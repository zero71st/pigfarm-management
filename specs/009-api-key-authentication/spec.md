# Feature Specification: API-Key Authentication (Option B) — Admin-managed users

**Feature Branch**: `009-api-key-authentication`  
**Created**: October 8, 2025  
**Status**: Draft  
**Type**: Security & Authentication Infrastructure  
**Input**: Implement API-key based authentication with admin-managed user system for secure access control

## Execution Flow (main)
```
1. Parse requirements for API-key authentication system
   → Extracted: admin-managed users, API-key generation, role-based access, secure storage
2. Extract key concepts from authentication domain
   → Actors: system administrators, end users, API consumers
   → Actions: authenticate, authorize, manage users, generate keys, revoke access
   → Data: user credentials, API keys, roles, permissions
   → Constraints: admin-only user management, secure key storage, no self-registration
3. Define security requirements and flows
   → Authentication: username/password → API key generation
   → Authorization: X-Api-Key header validation → role-based access control
   → User management: admin creates/updates/deactivates users
4. Identify system boundaries and integration points
   → Database: User and ApiKey entities with proper relationships
   → Middleware: API key validation and claims population
   → Client: secure key storage and automatic header injection
5. Generate comprehensive functional and security requirements
6. Define data models and API contracts
7. Establish operational and security guidelines
```

---

## ⚡ Quick Guidelines
- ✅ Focus on WHAT the system needs and WHY (business/security requirements)
- ❌ Avoid HOW to implement (no specific tech stack details in spec)
- 🔒 Security-first approach with clear threat model considerations
- 👥 Written for business stakeholders and security reviewers

---

## Business Context

### Problem Statement
The PigFarm Management System currently lacks proper authentication and authorization mechanisms, creating security risks and limiting the ability to control access to sensitive farm data and operations. The system needs a secure, manageable authentication approach that balances security with operational simplicity.


### Solution Approach
Implement an API-key based authentication system where administrators manage all user accounts and generate secure API keys. The system includes a production-safe admin seeder:
- On startup, if no admin exists, creates one using environment variables (ADMIN_USERNAME, ADMIN_EMAIL, ADMIN_PASSWORD, ADMIN_APIKEY).
- In production, requires secrets to be set (no secret printing); in development, generates and logs them once for convenience.
- For Railway/Postgres, set secrets in Railway variables before first deploy; seeder will not print secrets in production.
This approach provides strong security controls while maintaining operational simplicity for small to medium farm operations.

### Success Criteria
- **Security**: All sensitive operations require valid authentication and appropriate authorization
- **Manageability**: Administrators can easily create, manage, and revoke user access
- **Usability**: End users have a simple login experience with persistent authentication
- **Auditability**: All authentication and authorization events are logged and traceable
- **Scalability**: System can grow from single-admin to multi-admin scenarios

## User Scenarios & Testing

### Primary Actors
- **System Administrator**: Manages user accounts, generates API keys, controls access
- **Farm Manager**: Daily operations user with elevated permissions
- **Farm Worker**: Limited access user for basic operations
- **External API Consumer**: Automated systems or integrations requiring programmatic access

### Core User Journeys

#### 1. Administrator Manages Users
**As a System Administrator, I want to manage user accounts so that I can control who has access to the farm management system.**

**Steps:**
1. Administrator logs into admin interface with their credentials
2. Navigates to user management section
3. Creates new user account with username, email, and assigned roles
4. Generates initial API key for the user
5. Securely communicates credentials to the user
6. Monitors user activity and manages ongoing access

**Acceptance Criteria:**
- ✅ Admin can create user accounts with username, email, and role assignments
- ✅ Admin can generate multiple API keys per user for different devices/applications
- ✅ Admin can view all active users and their current API keys
- ✅ Admin can deactivate users or revoke specific API keys instantly
- ✅ All administrative actions are logged with timestamp and admin identity

#### 2. User Authentication Flow
**As a Farm Manager, I want to log into the system securely so that I can manage farm operations.**

**Steps:**
1. User enters username and password on login page
2. System validates credentials and generates new API key
3. User receives API key (shown once for security)
4. User's client application stores API key securely
5. Subsequent requests include API key in X-Api-Key header
6. System validates API key and grants access based on user roles

**Acceptance Criteria:**
- ✅ Valid username/password combination generates a new API key
- ✅ API key is displayed to user exactly once after generation
- ✅ Invalid credentials result in clear error message without revealing user existence
- ✅ API keys include expiration date and can be configured for different lifespans
- ✅ User can have multiple active API keys for different devices

#### 3. API Key Usage and Validation
**As a Farm Worker, I want my authentication to work seamlessly so that I can focus on farm tasks without authentication interruptions.**

**Steps:**
1. User's application automatically includes API key in request headers
2. System middleware validates API key against stored hash
3. System loads user identity and role information
4. Request proceeds with populated security context
5. Endpoint authorization checks user roles against required permissions
6. Operation executes or returns authorization error

**Acceptance Criteria:**
- ✅ Valid API key grants access to appropriate system functions
- ✅ Invalid or expired API key results in 401 Unauthorized response
- ✅ Role-based authorization prevents access to unauthorized functions
- ✅ API key validation is performant and doesn't impact system responsiveness
- ✅ Security context is properly populated for all authenticated requests

#### 4. Security Incident Response
**As a System Administrator, I want to quickly revoke access when needed so that I can respond to security incidents.**

**Steps:**
1. Administrator receives report of suspicious activity or security concern
2. Quickly identifies affected user account(s)
3. Immediately revokes all API keys for affected user(s)
4. Reviews audit logs to assess impact
5. Generates new credentials if user access should be restored
6. Documents incident and response actions

**Acceptance Criteria:**
- ✅ API key revocation takes effect immediately across all system components
- ✅ Revoked keys cannot be used for any system access
- ✅ Audit trail shows all key generation, usage, and revocation events
- ✅ Administrator can bulk-revoke keys or revoke individual keys selectively
- ✅ System provides clear feedback about revocation success

### Edge Cases and Error Scenarios

#### Concurrent Key Usage
- **Scenario**: Same API key used simultaneously from multiple locations
- **Expected**: System allows concurrent usage but logs all access points
- **Security**: Monitor for unusual geographic or temporal patterns

#### Key Rotation
- **Scenario**: User needs to rotate API key due to potential compromise
- **Expected**: Admin can generate new key while old key remains valid for transition period
- **Security**: Clear expiration policy and transition management

#### System Recovery
- **Scenario**: Authentication system database corruption or loss
- **Expected**: Recovery procedures that don't compromise security
- **Security**: Secure backup and recovery processes with admin verification

## Functional Requirements

### FR-1: User Account Management
**Priority**: High | **Complexity**: Medium

The system shall provide comprehensive user account management capabilities exclusively available to administrators.

**Requirements:**
- FR-1.1: Administrators can create user accounts with unique username and email
- FR-1.2: User passwords are securely hashed using industry-standard algorithms
- FR-1.3: Users can be assigned multiple roles (Admin, Manager, Worker, Viewer)
- FR-1.4: User accounts can be activated, deactivated, and reactivated
- FR-1.5: Account deactivation immediately invalidates all associated API keys
- FR-1.6: System maintains audit trail of all account modifications

### FR-2: API Key Generation and Management
**Priority**: High | **Complexity**: Medium

The system shall generate and manage API keys for user authentication with proper security controls.

**Requirements:**
- FR-2.1: System generates cryptographically secure random API keys
- FR-2.2: API keys are stored as salted hashes, never in plain text
- FR-2.3: Raw API key is displayed to admin/user exactly once after generation
- FR-2.4: Each user can have multiple active API keys simultaneously
- FR-2.5: API keys include configurable expiration dates
- FR-2.6: Expired API keys are automatically invalidated
- FR-2.7: Administrators can manually revoke any API key immediately

### FR-3: Authentication Middleware
**Priority**: High | **Complexity**: High

The system shall provide middleware to validate API keys and establish security context for requests.

**Requirements:**
- FR-3.1: Middleware validates X-Api-Key header on all protected endpoints
- FR-3.2: API key validation includes hash comparison and expiration check
- FR-3.3: Valid API key populates security context with user identity and roles
- FR-3.4: Invalid or missing API key results in 401 Unauthorized response
- FR-3.5: Middleware performance does not significantly impact request latency
- FR-3.6: Failed authentication attempts are logged for security monitoring

### FR-4: Role-Based Authorization
**Priority**: High | **Complexity**: Medium

The system shall enforce role-based access control for all protected operations.

**Requirements:**
- FR-4.1: Endpoints can require specific roles for access
- FR-4.2: Role inheritance supports Admin > Manager > Worker > Viewer hierarchy
- FR-4.3: Insufficient role permissions result in 403 Forbidden response
- FR-4.4: Role checks are enforced consistently across all system components
- FR-4.5: Authorization decisions are logged for audit purposes

### FR-5: Client Integration
**Priority**: Medium | **Complexity**: Medium

The system shall provide seamless client-side integration for API key authentication.

**Requirements:**
- FR-5.1: Client applications can securely store API keys locally
- FR-5.2: HTTP requests automatically include API key in appropriate header
- FR-5.3: Authentication failures trigger appropriate user feedback
- FR-5.4: Client supports multiple authentication contexts (multi-user scenarios)
- FR-5.5: API key storage follows platform-specific security best practices

### FR-6: Administrative Interface
**Priority**: Medium | **Complexity**: Medium

The system shall provide web-based administrative interface for user and key management.

**Requirements:**
- FR-6.1: Admin interface for creating and managing user accounts
- FR-6.2: API key generation interface with one-time display
- FR-6.3: User and API key listing with status information
- FR-6.4: Bulk operations for user management (activate/deactivate multiple users)
- FR-6.5: Audit log viewing and searching capabilities

## Security Requirements

### SR-1: Credential Protection
**Priority**: Critical | **Complexity**: Medium

**Requirements:**
- SR-1.1: Passwords stored using BCrypt or equivalent salted hash algorithm
- SR-1.2: API keys stored using SHA-256 or stronger hash algorithm
- SR-1.3: No plain text credentials stored anywhere in the system
- SR-1.4: Raw API keys transmitted only over HTTPS
- SR-1.5: API key generation uses cryptographically secure random number generator

### SR-2: Access Control
**Priority**: Critical | **Complexity**: High

**Requirements:**
- SR-2.1: All administrative functions restricted to Admin role
- SR-2.2: User cannot elevate their own privileges
- SR-2.3: API key cannot be used to generate additional API keys
- SR-2.4: Authentication required for all non-public endpoints
- SR-2.5: Session isolation prevents cross-user data access

### SR-3: Audit and Monitoring
**Priority**: High | **Complexity**: Medium

**Requirements:**
- SR-3.1: All authentication attempts logged with timestamp and source
- SR-3.2: All administrative actions logged with actor and target
- SR-3.3: API key usage patterns monitored for anomalies
- SR-3.4: Failed authentication attempts trigger monitoring alerts
- SR-3.5: Audit logs are tamper-evident and securely stored

### SR-4: Incident Response
**Priority**: High | **Complexity**: Low

**Requirements:**
- SR-4.1: Immediate API key revocation capability
- SR-4.2: Bulk revocation for compromised user accounts
- SR-4.3: Emergency admin access procedures
- SR-4.4: Forensic data preservation for security investigations

## Key Entities

### User Entity
**Purpose**: Represents system users with authentication credentials and role assignments

**Core Attributes:**
- **Identity**: Unique identifier, username, email address
- **Security**: Password hash, active status, creation timestamp
- **Authorization**: Role assignments, permission flags
- **Audit**: Created by, modified by, last login timestamp

**Relationships:**
- **One-to-Many**: User → API Keys
- **Many-to-Many**: User → Roles (if role system is normalized)

**Business Rules:**
- Username must be unique across system
- Email must be unique and valid format
- Password must meet complexity requirements
- Deactivated users cannot authenticate
- User deletion is soft delete with full audit trail

### API Key Entity
**Purpose**: Represents authentication tokens with associated metadata

**Core Attributes:**
- **Identity**: Unique identifier, hashed key value
- **Ownership**: Associated user identifier
- **Lifecycle**: Creation timestamp, expiration timestamp, revocation timestamp
- **Authorization**: Role snapshot, active status
- **Audit**: Created by, usage statistics

**Relationships:**
- **Many-to-One**: API Key → User
- **One-to-Many**: API Key → Usage Logs (if detailed tracking needed)

**Business Rules:**
- Hashed key must be unique across all keys (active and revoked)
- Expired keys cannot be used for authentication
- Revoked keys cannot be reactivated
- Key expiration automatically invalidates authentication
- Key usage updates last accessed timestamp

### Role Entity (Optional)
**Purpose**: Defines permission sets and authorization levels

**Core Attributes:**
- **Identity**: Role name, display name, description
- **Permissions**: Permission flags, hierarchical level
- **System**: Built-in flag, active status

**Business Rules:**
- System roles cannot be deleted
- Role hierarchy enforces permission inheritance
- Role modifications affect all associated users immediately

## API Contracts

### Authentication Endpoints

#### POST /api/auth/login
**Purpose**: Authenticate user and generate new API key

**Request Body:**
```json
{
  "username": "string",
  "password": "string",
  "keyLabel": "string (optional)",
  "expirationDays": "number (optional, default: 30)"
}
```

**Response (Success - 200):**
```json
{
  "apiKey": "string (raw key, shown once)",
  "expiresAt": "datetime",
  "user": {
    "id": "guid",
    "username": "string",
    "roles": ["string"]
  }
}
```

**Response (Failure - 401):**
```json
{
  "error": "Invalid credentials",
  "timestamp": "datetime"
}
```

### Administrative Endpoints

#### POST /api/admin/users
**Purpose**: Create new user account (Admin only)

**Request Body:**
```json
{
  "username": "string",
  "email": "string",
  "password": "string",
  "roles": ["string"],
  "isActive": "boolean (default: true)"
}
```

**Response (Success - 201):**
```json
{
  "id": "guid",
  "username": "string",
  "email": "string",
  "roles": ["string"],
  "isActive": "boolean",
  "createdAt": "datetime"
}
```

#### GET /api/admin/users
**Purpose**: List all users with pagination (Admin only)

**Query Parameters:**
- `page`: Page number (default: 1)
- `pageSize`: Items per page (default: 20)
- `isActive`: Filter by active status (optional)
- `role`: Filter by role (optional)

**Response (Success - 200):**
```json
{
  "users": [
    {
      "id": "guid",
      "username": "string",
      "email": "string",
      "roles": ["string"],
      "isActive": "boolean",
      "lastLogin": "datetime",
      "activeApiKeys": "number"
    }
  ],
  "pagination": {
    "currentPage": "number",
    "totalPages": "number",
    "totalCount": "number"
  }
}
```

#### POST /api/admin/users/{userId}/apikeys
**Purpose**: Generate new API key for user (Admin only)

**Request Body:**
```json
{
  "label": "string (optional)",
  "expirationDays": "number (optional, default: 30)"
}
```

**Response (Success - 201):**
```json
{
  "apiKey": "string (raw key, shown once)",
  "keyId": "guid",
  "expiresAt": "datetime",
  "label": "string"
}
```

#### DELETE /api/admin/apikeys/{keyId}
**Purpose**: Revoke specific API key (Admin only)

**Response (Success - 204):** No content

#### GET /api/admin/users/{userId}/apikeys
**Purpose**: List API keys for specific user (Admin only)

**Response (Success - 200):**
```json
{
  "apiKeys": [
    {
      "id": "guid",
      "label": "string",
      "createdAt": "datetime",
      "expiresAt": "datetime",
      "lastUsed": "datetime",
      "isActive": "boolean"
    }
  ]
}
```

## Non-Functional Requirements

### Performance Requirements
- **NFR-1**: API key validation must complete within 50ms for 95% of requests
- **NFR-2**: System must support 1000 concurrent authenticated users
- **NFR-3**: Authentication middleware must not increase request latency by more than 10ms
- **NFR-4**: Database queries for authentication must use proper indexing

### Security Requirements
- **NFR-5**: All authentication communications must use HTTPS/TLS 1.2+
- **NFR-6**: API keys must have minimum entropy of 256 bits
- **NFR-7**: Password hashing must use work factor appropriate for current hardware
- **NFR-8**: Audit logs must be tamper-evident and securely stored

### Reliability Requirements
- **NFR-9**: Authentication system must maintain 99.9% uptime
- **NFR-10**: API key revocation must take effect within 5 seconds system-wide
- **NFR-11**: System must gracefully handle authentication database connectivity issues
- **NFR-12**: Recovery procedures must restore authentication capability within 1 hour

### Usability Requirements
- **NFR-13**: Admin interface must be intuitive for non-technical administrators
- **NFR-14**: API key display must clearly indicate one-time visibility
- **NFR-15**: Error messages must be clear without revealing sensitive information
- **NFR-16**: Client integration must not require complex configuration

## Dependencies and Assumptions

### System Dependencies
- **Database**: Entity Framework Core with SQL database backend
- **Web Framework**: ASP.NET Core with middleware pipeline
- **Client Platform**: Blazor WebAssembly with local storage capabilities
- **Security Libraries**: BCrypt.NET or ASP.NET Core Identity for password hashing

### External Dependencies
- **HTTPS/TLS**: Secure transport layer for all communications
- **Time Synchronization**: Accurate system time for token expiration
- **Storage Security**: Secure database storage with encryption at rest
- **Email Service**: For password reset and notification capabilities (future)

### Key Assumptions
- **Single Database**: All authentication data stored in primary application database
- **Admin Bootstrap**: Initial admin account created during system deployment
- **Client Storage**: Client applications can securely store API keys locally
- **Network Security**: Internal network communications are trusted
- **Backup Strategy**: Authentication data included in regular backup procedures

## Risk Assessment

### High-Risk Areas
- **API Key Compromise**: Raw keys visible during generation and transmission
- **Database Security**: All authentication data centralized in single database
- **Admin Account**: Single point of failure for initial system access
- **Client Storage**: API keys stored in potentially insecure client storage

### Mitigation Strategies
- **Key Rotation**: Regular key rotation policies and capabilities
- **Access Monitoring**: Comprehensive logging and anomaly detection
- **Recovery Procedures**: Multiple admin accounts and emergency access procedures
- **Client Security**: Platform-specific secure storage implementations

### Security Considerations
- **Threat Model**: Primary threats include credential theft, privilege escalation, and API key compromise
- **Attack Vectors**: Brute force attacks, social engineering, client-side storage attacks
- **Defense in Depth**: Multiple security layers including network, application, and data security
- **Incident Response**: Clear procedures for security incident detection and response

## Operational Requirements

### Deployment Considerations
- **Environment Configuration**: Secure configuration management for production secrets
- **Initial Setup**: Bootstrap procedures for first admin account creation
- **Database Migration**: Secure migration procedures for authentication schema changes
- **Monitoring Setup**: Authentication-specific monitoring and alerting configuration

### Maintenance Procedures
- **Key Rotation**: Regular API key rotation policies and automated cleanup
- **User Lifecycle**: Procedures for user onboarding, role changes, and offboarding
- **Audit Review**: Regular review of authentication logs and user access patterns
- **Security Updates**: Procedures for applying security patches and updates

### Backup and Recovery
- **Data Backup**: Authentication data included in regular backup procedures
- **Recovery Testing**: Regular testing of authentication system recovery procedures
- **Emergency Access**: Procedures for emergency admin access during system issues
- **Data Retention**: Policies for retaining and purging authentication audit data

---

## Implementation Notes

### Technology Constraints
- Must integrate with existing PigFarmManagement codebase
- Must follow established project patterns and conventions
- Must use Entity Framework Core for data persistence
- Must be compatible with Blazor WebAssembly client architecture

### Integration Points
- **Database Schema**: New authentication tables with proper relationships
- **Middleware Pipeline**: Authentication middleware in ASP.NET Core pipeline
- **Client Services**: HTTP client integration with automatic header injection
- **Admin Interface**: Integration with existing admin UI patterns

### Future Extensibility
- **External Identity**: Design allows future integration with external identity providers
- **Additional Auth Methods**: Architecture supports adding additional authentication mechanisms
- **Advanced RBAC**: Foundation for more sophisticated role and permission systems
- **API Versioning**: Authentication system designed to support API versioning

---

## Validation Criteria

### Acceptance Testing
- **Functional Testing**: All user scenarios execute successfully
- **Security Testing**: Penetration testing of authentication mechanisms
- **Performance Testing**: Authentication system meets performance requirements
- **Integration Testing**: Seamless integration with existing system components

### Security Validation
- **Code Review**: Security-focused code review by qualified personnel
- **Vulnerability Assessment**: Automated and manual security vulnerability testing
- **Compliance Check**: Verification against security best practices and standards
- **Audit Trail**: Verification of complete audit trail functionality

### Operational Validation
- **Admin Procedures**: Validation of all administrative procedures and workflows
- **Recovery Testing**: Testing of all backup and recovery procedures
- **Monitoring**: Validation of monitoring and alerting capabilities
- **Documentation**: Complete and accurate operational documentation

---

## Success Metrics

### Security Metrics
- Zero successful unauthorized access attempts
- 100% of authentication events properly logged
- API key revocation effective within SLA timeframes
- No plain text credentials stored in system

### Performance Metrics
- Authentication latency within specified limits
- System availability meets or exceeds SLA requirements
- Concurrent user capacity meets or exceeds requirements
- Database performance remains within acceptable parameters

### Usability Metrics
- Admin task completion rates above 95%
- User authentication success rates above 98%
- Support ticket volume related to authentication below baseline
- User satisfaction scores meet or exceed targets

---

## Execution Status
*To be updated during implementation*

- [ ] Specification approved by stakeholders
- [ ] Security review completed
- [ ] Technical design completed
- [ ] Implementation plan finalized
- [ ] Development phase initiated
- [ ] Testing phase completed
- [ ] Security validation completed
- [ ] Production deployment completed

---

*This specification serves as the authoritative requirements document for the API-Key Authentication (Option B) implementation. All implementation decisions should align with the requirements and constraints outlined in this document.*