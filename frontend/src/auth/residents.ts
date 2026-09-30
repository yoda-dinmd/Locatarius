export type Resident = {
  id: number | string;
  name: string;
  apartment: string;
  role: "Proprietar" | "Chiriaș";
  status: "Activ" | "Inactiv";
};

type ResidentDto = {
  id: string;
  firstName: string;
  lastName: string;
  apartment: string;
  isActive: boolean;
};

export async function fetchResidents(signal?: AbortSignal): Promise<Resident[]> {
  const response = await fetch("/api/residents", {
    credentials: "include",
    headers: { Accept: "application/json" },
    signal,
  });

  if (!response.ok) {
    throw new Error(`Residents request failed with status ${response.status}`);
  }

  const residents = (await response.json()) as ResidentDto[];

  return residents.map((resident) => ({
    id: resident.id,
    name: `${resident.firstName} ${resident.lastName}`,
    apartment: resident.apartment,
    // The API does not distinguish owners from tenants yet.
    role: "Chiriaș",
    status: resident.isActive ? "Activ" : "Inactiv",
  }));
}
