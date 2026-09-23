export type UserRole = "admin" | "resident";

export type MockUser = {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  buildingId: string;
  password: string;
  mustChangePassword: boolean;
};

export type SessionUser = Omit<
  MockUser,
  "password" | "mustChangePassword"
>;

export type AuthErrorCode =
  | "INVALID_CREDENTIALS"
  | "VALIDATION_FAILED"
  | "PASSWORD_MISMATCH"
  | "SESSION_EXPIRED"
  | "USER_NOT_FOUND";

export type AuthFailure = {
  ok: false;
  code: AuthErrorCode;
  message: string;
};

export type LoginResult =
  | {
      ok: true;
      next: "app" | "change_password";
      user: SessionUser;
    }
  | AuthFailure;

export type ChangePasswordResult = { ok: true } | AuthFailure;

const USERS_KEY = "locatarius-mock-users";
const SESSION_KEY = "locatarius-mock-session";
const PENDING_USER_KEY = "locatarius-mock-pending-user";

const initialUsers: MockUser[] = [
  {
    id: "admin-1",
    name: "Victor Munteanu",
    email: "admin@locatarius.test",
    role: "admin",
    buildingId: "BLD-01",
    password: "AdminPassword123!",
    mustChangePassword: false,
  },
  {
    id: "resident-1",
    name: "Ana Popescu",
    email: "ana@locatarius.test",
    role: "resident",
    buildingId: "BLD-01",
    password: "ResidentTemp123!",
    mustChangePassword: true,
  },
  //changed password: anapopescutemp123
    {
    id: "resident-2",
    name: "Ana Munteanu",
    email: "anam@locatarius.test",
    role: "resident",
    buildingId: "BLD-02",
    password: "ResidentTemp123!",
    mustChangePassword: true,
  },
  //changed password: anamunteanutemp123
      {
    id: "resident-3",
    name: "Ion Munteanu",
    email: "ion@locatarius.test",
    role: "resident",
    buildingId: "BLD-03",
    password: "ResidentTemp123!",
    mustChangePassword: true,
  },
];

function readUsers(): MockUser[] {
  const storedUsers = localStorage.getItem(USERS_KEY);

  if (!storedUsers) {
    localStorage.setItem(USERS_KEY, JSON.stringify(initialUsers));
    return initialUsers;
  }

  return JSON.parse(storedUsers) as MockUser[];
}

function writeUsers(users: MockUser[]) {
  localStorage.setItem(USERS_KEY, JSON.stringify(users));
}

function toSessionUser(user: MockUser): SessionUser {
  return {
    id: user.id,
    name: user.name,
    email: user.email,
    role: user.role,
    buildingId: user.buildingId,
  };
}

export async function login(
  email: string,
  password: string,
): Promise<LoginResult> {
  await new Promise((resolve) => setTimeout(resolve, 400));

  const user = readUsers().find(
    (candidate) =>
      candidate.email.toLowerCase() === email.trim().toLowerCase(),
  );

  if (!user || user.password !== password) {
    return {
      ok: false,
      code: "INVALID_CREDENTIALS",
      message: "Email or password is incorrect.",
    };
  }

  const sessionUser = toSessionUser(user);

  if (user.mustChangePassword) {
    localStorage.removeItem(SESSION_KEY);
    localStorage.setItem(PENDING_USER_KEY, user.id);

    return {
      ok: true,
      next: "change_password",
      user: sessionUser,
    };
  }

  localStorage.removeItem(PENDING_USER_KEY);
  localStorage.setItem(SESSION_KEY, JSON.stringify(sessionUser));

  return {
    ok: true,
    next: "app",
    user: sessionUser,
  };
}

export function getPendingUser(): SessionUser | null {
  const pendingUserId = localStorage.getItem(PENDING_USER_KEY);

  if (!pendingUserId) {
    return null;
  }

  const user = readUsers().find(
    (candidate) => candidate.id === pendingUserId,
  );

  return user ? toSessionUser(user) : null;
}

export async function changePassword(
  newPassword: string,
  confirmation: string,
): Promise<ChangePasswordResult> {
  await new Promise((resolve) => setTimeout(resolve, 400));

  if (newPassword.length < 15 || newPassword.length > 128) {
    return {
      ok: false,
      code: "VALIDATION_FAILED",
      message: "Your new password must contain 15-128 characters.",
    };
  }

  if (newPassword !== confirmation) {
    return {
      ok: false,
      code: "PASSWORD_MISMATCH",
      message: "The new passwords do not match.",
    };
  }

  const pendingUserId = localStorage.getItem(PENDING_USER_KEY);

  if (!pendingUserId) {
    return {
      ok: false,
      code: "SESSION_EXPIRED",
      message: "Your password-change session has expired.",
    };
  }

  const users = readUsers();
  const userIndex = users.findIndex(
    (user) => user.id === pendingUserId,
  );

  if (userIndex === -1) {
    return {
      ok: false,
      code: "USER_NOT_FOUND",
      message: "The account could not be found.",
    };
  }

  const updatedUser: MockUser = {
    ...users[userIndex],
    password: newPassword,
    mustChangePassword: false,
  };

  users[userIndex] = updatedUser;
  writeUsers(users);

  const sessionUser = toSessionUser(updatedUser);

  localStorage.setItem(SESSION_KEY, JSON.stringify(sessionUser));
  localStorage.removeItem(PENDING_USER_KEY);

  return { ok: true };
}

export function getSession(): SessionUser | null {
  const storedSession = localStorage.getItem(SESSION_KEY);

  if (!storedSession) {
    return null;
  }

  return JSON.parse(storedSession) as SessionUser;
}

export function getBuildingIdForUser(userId: string): string | null {
  return readUsers().find((user) => user.id === userId)?.buildingId ?? null;
}

export function signOut() {
  localStorage.removeItem(SESSION_KEY);
}