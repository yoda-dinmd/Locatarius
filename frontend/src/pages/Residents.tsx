import { useEffect, useState } from "react";
import type { SessionUser } from "../auth/auth";
import { fetchResidents, type Resident } from "../auth/residents";
import IssueLayout from "./IssueLayout";
import residentsMock from "../data/residentsMock.json";
import "../styles/Residents.css";

type ResidentsProps = {
  user: SessionUser;
  residents?: readonly Resident[];
};

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
  residents: providedResidents,
}: ResidentsProps) {
  const [residents, setResidents] = useState<readonly Resident[]>(
    providedResidents ?? (residentsMock as Resident[]),
  );
  const [loadFailed, setLoadFailed] = useState(false);

  useEffect(() => {
    if (providedResidents) return;

    const controller = new AbortController();

    fetchResidents(controller.signal)
      .then((loaded) => {
        setResidents(loaded);
        setLoadFailed(false);
      })
      .catch((error: unknown) => {
        if (!controller.signal.aborted) {
          console.error(error);
          setLoadFailed(true);
        }
      });

    return () => controller.abort();
  }, [providedResidents]);

  function handleAddResident() {
    console.info("Adaugă locatar");
  }

  function handleEditResident(id: Resident["id"]) {
    console.info("Editează locatarul", id);
  }

  function handleDeleteResident(id: Resident["id"]) {
    console.info("Șterge locatarul", id);
  }

  return (
    <IssueLayout user={user} activePage="residents">
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

          {loadFailed && (
            <p className="residents-intro" role="alert">
              Lista reală nu a putut fi încărcată (este necesară o sesiune de
              administrator pe server). Se afișează date demonstrative.
            </p>
          )}

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
    </IssueLayout>
  );
}
