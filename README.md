# GastronomIQ

**GastronomIQ** is a Recipe Development Tool designed for culinary professionals. It streamlines recipe creation, ingredient management, nutritional analysis, and collaborative recipe refinement—all in one platform.

## Features

- 📝 **Recipe Management** – Create, edit, and organize recipes with detailed instructions
- 🥘 **Ingredient Tracking** – Manage ingredient inventory and sourcing
- 📊 **Nutritional Analysis** – Automatic nutritional information calculation
- 🔍 **Search & Filter** – Find recipes by cuisine, dietary restrictions, preparation time
- 👥 **Collaboration** – Team comments, version history, and approval workflows
- 📱 **Responsive Design** – Works on desktop, tablet, and mobile devices

## Tech Stack

- **Frontend:** Node.js + React + Next.js (App Router)
- **Backend:** Node.js + Express.js / Python + FastAPI (choose one)
- **Database:** PostgreSQL
- **Authentication:** JWT + OAuth2 (optional)
- **UI Framework:** Tailwind CSS
- **Testing:** Jest (frontend), pytest (backend)

## Getting Started

### Prerequisites

- Node.js 18+ (or Python 3.10+)
- npm or yarn
- PostgreSQL 14+
- Git

### Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/CulinariaProfessionali/GastronomIQ.git
   cd GastronomIQ
   ```

2. **Install dependencies:**
   ```bash
   npm install
   # or
   yarn install
   ```

3. **Set up environment variables:**
   ```bash
   cp .env.example .env.local
   ```
   Edit `.env.local` with your configuration (database URL, API keys, etc.)

4. **Initialize the database:**
   ```bash
   npm run db:migrate
   npm run db:seed
   ```

5. **Start the development server:**
   ```bash
   npm run dev
   ```

   The application will be available at `http://localhost:3000`

### Running Tests

```bash
# Run all tests
npm run test

# Run tests in watch mode
npm run test:watch

# Generate coverage report
npm run test:coverage
```

### Docker Setup (Optional)

```bash
docker-compose up -d
npm run dev
```

## Project Structure

```
GastronomIQ/
├── README.md                    # This file
├── ARCHITECTURE.md              # System design and technical decisions
├── package.json                 # Node.js dependencies
├── package-lock.json            # Dependency lock file
├── .env.example                 # Environment variables template
├── .gitignore                   # Git ignore rules
├── .eslintrc.json               # ESLint configuration
├── jest.config.js               # Jest test configuration
├── tsconfig.json                # TypeScript configuration (if using TS)
├── docker-compose.yml           # Docker Compose setup
│
├── src/
│   ├── api/                     # API routes and controllers
│   │   ├── recipes/
│   │   ├── ingredients/
│   │   ├── users/
│   │   └── health.ts            # Health check endpoint
│   │
│   ├── services/                # Business logic layer
│   │   ├── RecipeService.ts
│   │   ├── IngredientService.ts
│   │   ├── NutritionService.ts
│   │   └── AuthService.ts
│   │
│   ├── models/                  # Data models & database schemas
│   │   ├── Recipe.ts
│   │   ├── Ingredient.ts
│   │   ├── User.ts
│   │   └── db.ts                # Database connection
│   │
│   ├── middleware/              # Express middleware
│   │   ├── auth.ts
│   │   ├── errorHandler.ts
│   │   └── validation.ts
│   │
│   ├── utils/                   # Utilities and helpers
│   │   ├── logger.ts
│   │   ├── validators.ts
│   │   ├── formatters.ts
│   │   └── constants.ts
│   │
│   ├── config/                  # Configuration files
│   │   ├── database.ts
│   │   └── server.ts
│   │
│   └── index.ts                 # Application entry point
│
├── tests/
│   ├── unit/                    # Unit tests
│   │   ├── services/
│   │   └── utils/
│   │
│   ├── integration/             # Integration tests
│   │   └── api/
│   │
│   ├── fixtures/                # Test data and mocks
│   │   └── recipes.json
│   │
│   └── setup.ts                 # Test configuration
│
├── migrations/                  # Database migrations
│   ├── 001_initial_schema.sql
│   └── 002_add_nutrition_table.sql
│
├── scripts/
│   ├── bootstrap.sh             # Project setup script
│   ├── seed-db.ts               # Database seeding
│   └── lint-fix.sh              # Auto-fix linting issues
│
├── docs/
│   ├── ARCHITECTURE.md          # System design overview
│   ├── API.md                   # API documentation
│   ├── DATABASE.md              # Database schema & queries
│   ├── CONTRIBUTING.md          # Contribution guidelines
│   └── DEPLOYMENT.md            # Deployment instructions
│
└── .github/
    └── workflows/
        ├── ci.yml               # CI pipeline
        └── deploy.yml           # CD pipeline
```

## Environment Variables

Copy `.env.example` to `.env.local` and configure:

```env
# Server
PORT=3000
NODE_ENV=development

# Database
DATABASE_URL=postgresql://user:password@localhost:5432/gastronomiq

# JWT
JWT_SECRET=your-secret-key-here

# OAuth (optional)
GOOGLE_CLIENT_ID=your-google-client-id
GOOGLE_CLIENT_SECRET=your-google-client-secret

# API
API_BASE_URL=http://localhost:3000/api
```

## Available Scripts

```bash
npm run dev              # Start development server
npm run build            # Build for production
npm run start            # Start production server
npm run test             # Run test suite
npm run test:watch      # Run tests in watch mode
npm run test:coverage   # Generate coverage report
npm run lint             # Run ESLint
npm run lint:fix        # Fix linting issues
npm run db:migrate      # Run database migrations
npm run db:seed         # Seed database with sample data
npm run format           # Format code with Prettier
```

## API Documentation

See [`docs/API.md`](docs/API.md) for detailed endpoint documentation.

## Contributing

1. Create a feature branch: `git checkout -b feature/your-feature`
2. Commit changes: `git commit -m 'Add your feature'`
3. Push to branch: `git push origin feature/your-feature`
4. Open a Pull Request

See [`docs/CONTRIBUTING.md`](docs/CONTRIBUTING.md) for detailed guidelines.

## Database Schema

See [`docs/DATABASE.md`](docs/DATABASE.md) for schema details and query examples.

## Deployment

For production deployment instructions, see [`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md).

## License

This project is licensed under the MIT License. See [`LICENSE`](LICENSE) file for details.

## Support

For issues, feature requests, or questions:
- 📧 Email: support@gastronomiq.com
- 💬 GitHub Issues: [Create an issue](https://github.com/CulinariaProfessionali/GastronomIQ/issues)
- 📚 Documentation: [Explore docs](docs/)

---

**Happy recipe developing! 👨‍🍳👩‍🍳**
