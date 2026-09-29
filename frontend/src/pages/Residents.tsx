import type { SessionUser } from "../auth/auth";
import { signOut } from "../auth/auth";
import residentsMock from "../data/residentsMock.json";
import "../styles/Dashboard.css";
import "../styles/Residents.css";

type ResidentsProps = {
  user: SessionUser;
  residents?: readonly Resident[];
};

type Resident = {
  id: number;
  name: string;
  apartment: string;
  role: "Proprietar" | "Chiriaș";
  status: "Activ" | "Inactiv";
};

function getInitials(name: string) {
  return name
    .split(" ")
    .map((part) => part[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();
}

function EditIcon() {
  return (
    <svg aria-hidden="true" viewBox="0 0 20 20">
      <path d="M4 13.9V16h2.1l8.2-8.2-2.1-2.1L4 13.9Zm11.9-7.7a.7.7 0 0 0 0-1l-1.1-1.1a.7.7 0 0 0-1 0l-.9.9L15 7.1l.9-.9Z" />
    </svg>
  );
}

function DeleteIcon() {
  return (
    <svg aria-hidden="true" viewBox="0 0 20 20">
      <path d="M6.2 16.3h7.6l.7-9.5h-9l.7 9.5Zm5.5-11V4H8.3v1.3H4.7v1.5h10.6V5.3h-3.6Z" />
    </svg>
  );
}

export default function Residents({
  user,
  residents = residentsMock as Resident[],
}: ResidentsProps) {
  function handleSignOut() {
    signOut();
    window.location.href = "/";
  }

  function handleAddResident() {
    console.info("Adaugă locatar");
  }

  function handleEditResident(id: number) {
    console.info("Editează locatarul", id);
  }

  function handleDeleteResident(id: number) {
    console.info("Șterge locatarul", id);
  }

  return (
    <main className="dashboard-page">
      <header className="dashboard-header">
        <a className="dashboard-brand" href="/dashboard">
          <span className="brand-mark">L</span>
          <span>Locatarius</span>
        </a>

        <div className="dashboard-user">
          <span className="user-initials">{getInitials(user.name)}</span>

          <span className="user-details">
            <strong>{user.name}</strong>
            <small>Administrator</small>
          </span>

          <button
            className="sign-out-button"
            type="button"
            onClick={handleSignOut}
          >
            Sign out
          </button>
        </div>
      </header>

      <div className="dashboard-layout">
        <aside className="dashboard-sidebar">
          <a className="dashboard-logo-text" href="/dashboard">
            Locatarius
          </a>

          <nav aria-label="Main navigation">
            <a className="dashboard-nav-link" href="/dashboard">
              Dashboard
            </a>
            <a
              className="dashboard-nav-link active"
              href="/residents"
              aria-current="page"
            >
              Locatari
            </a>
          </nav>
        </aside>

        <section className="dashboard-content residents-content">
          <div className="residents-heading">
            <div>
              <p className="dashboard-eyebrow">Administrare</p>
              <h1>Locatari</h1>
              <p className="residents-intro">
                Gestionează locatarii și apartamentele din clădire.
              </p>
            </div>

            <button
              className="resident-add-button"
              type="button"
              onClick={handleAddResident}
            >
              <span aria-hidden="true">+</span>
              Adaugă locatar
            </button>
          </div>

          <section className="residents-card" aria-labelledby="residents-list-title">
            <div className="residents-card-header">
              <div>
                <h2 id="residents-list-title">Lista locatarilor</h2>
                <p>{residents.length} locatari înregistrați</p>
              </div>
            </div>

            {residents.length === 0 ? (
              <div className="residents-empty">
                <h3>Nu există locatari</h3>
                <p>Adaugă primul locatar pentru a începe administrarea.</p>
              </div>
            ) : (
              <div className="residents-table-scroll">
                <table className="residents-table">
                  <thead>
                    <tr>
                      <th scope="col">Nume</th>
                      <th scope="col">Apartament</th>
                      <th scope="col">Rol</th>
                      <th scope="col">Status</th>
                      <th scope="col" className="residents-actions-heading">
                        Acțiuni
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {residents.map((resident) => (
                      <tr key={resident.id}>
                        <td>
                          <span className="resident-name">{resident.name}</span>
                        </td>
                        <td>{resident.apartment}</td>
                        <td>{resident.role}</td>
                        <td>
                          <span
                            className={`resident-status resident-status--${resident.status.toLowerCase()}`}
                          >
                            <span aria-hidden="true" />
                            {resident.status}
                          </span>
                        </td>
                        <td>
                          <div className="resident-actions">
                            <button
                              className="resident-action-button"
                              type="button"
                              onClick={() => handleEditResident(resident.id)}
                              aria-label={`Editează ${resident.name}`}
                            >
                              <EditIcon />
                              <span>Editează</span>
                            </button>
                            <button
                              className="resident-action-button resident-action-button--danger"
                              type="button"
                              onClick={() => handleDeleteResident(resident.id)}
                              aria-label={`Șterge ${resident.name}`}
                            >
                              <DeleteIcon />
                              <span>Șterge</span>
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </section>
      </div>
    </main>
  );
}
