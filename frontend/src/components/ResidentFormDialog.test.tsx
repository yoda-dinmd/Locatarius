import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ApiError } from "../api/client";
import ConfirmResidentDeactivation from "./ConfirmResidentDeactivation";
import ResidentFormDialog, {
  type ApartmentOptionDto,
  type ResidentFormValues,
} from "./ResidentFormDialog";

const apartments: ApartmentOptionDto[] = [
  { id: "apt-3a", apartment: "3A", building: "Bloc A" },
  { id: "apt-7b", apartment: "7B", building: "Bloc B" },
];

const resident: ResidentFormValues = {
  firstName: "Ana",
  lastName: "Popescu",
  email: "ana@example.test",
  apartmentId: "apt-3a",
  isActive: true,
};

function deferredPromise() {
  let resolve!: () => void;
  const promise = new Promise<void>((done) => {
    resolve = done;
  });
  return { promise, resolve };
}

async function fillValidCreateForm(
  user: ReturnType<typeof userEvent.setup>,
  password = "TemporaryPassword123!",
) {
  await user.type(screen.getByLabelText("Prenume"), "Maria");
  await user.type(screen.getByLabelText("Nume"), "Ionescu");
  await user.type(screen.getByLabelText("Email"), "maria@example.test");
  await user.selectOptions(screen.getByLabelText("Apartament"), "apt-3a");
  await user.type(screen.getByLabelText("Parolă temporară"), password);
  await user.type(screen.getByLabelText("Confirmă parola temporară"), password);
}

describe("ResidentFormDialog", () => {
  it("submits the backend create contract and clears credentials on success", async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    const onClose = vi.fn();

    render(
      <ResidentFormDialog
        open
        mode="create"
        apartments={apartments}
        onSubmit={onSubmit}
        onClose={onClose}
      />,
    );

    const dialog = screen.getByRole("dialog", { name: "Adaugă locatar" });
    expect(dialog).toHaveAttribute("aria-modal", "true");
    await waitFor(() => expect(screen.getByLabelText("Prenume")).toHaveFocus());

    await user.type(screen.getByLabelText("Prenume"), "Maria");
    await user.type(screen.getByLabelText("Nume"), "Ionescu");
    await user.type(screen.getByLabelText("Email"), "maria@example.test");
    await user.selectOptions(screen.getByLabelText("Apartament"), "apt-7b");

    const password = screen.getByLabelText("Parolă temporară");
    const confirmation = screen.getByLabelText("Confirmă parola temporară");
    expect(password).toHaveAttribute("minlength", "15");
    expect(password).toHaveAttribute("maxlength", "128");
    await user.type(password, "TemporaryPassword123!");
    await user.type(confirmation, "TemporaryPassword123!");
    await user.click(screen.getByRole("button", { name: "Salvează locatarul" }));

    await waitFor(() =>
      expect(onSubmit).toHaveBeenCalledWith({
        firstName: "Maria",
        lastName: "Ionescu",
        email: "maria@example.test",
        apartmentId: "apt-7b",
        temporaryPassword: "TemporaryPassword123!",
        confirmPassword: "TemporaryPassword123!",
      }),
    );
    expect(onClose).toHaveBeenCalledOnce();
    expect(password).toHaveValue("");
    expect(confirmation).toHaveValue("");
  });

  it("edits only safe resident fields and never renders password or role controls", async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn().mockResolvedValue(undefined);

    render(
      <ResidentFormDialog
        open
        mode="edit"
        resident={resident}
        apartments={apartments}
        onSubmit={onSubmit}
        onClose={vi.fn()}
      />,
    );

    expect(screen.queryByLabelText("Parolă temporară")).not.toBeInTheDocument();
    expect(screen.queryByText("Rol")).not.toBeInTheDocument();
    expect(screen.getByLabelText("Prenume")).toHaveValue("Ana");
    expect(screen.getByLabelText("Stare cont")).toHaveValue("true");

    await user.selectOptions(screen.getByLabelText("Stare cont"), "false");
    await user.click(screen.getByRole("button", { name: "Salvează modificările" }));

    await waitFor(() =>
      expect(onSubmit).toHaveBeenCalledWith({
        firstName: "Ana",
        lastName: "Popescu",
        email: "ana@example.test",
        apartmentId: "apt-3a",
        isActive: false,
      }),
    );
  });

  it("renders backend field errors beside their inputs and a conflict alert", async () => {
    const user = userEvent.setup();
    const onSubmit = vi
      .fn()
      .mockRejectedValueOnce(
        new ApiError(400, "VALIDATION_FAILED", "Date invalide", {
          firstName: ["Prenumele este obligatoriu."],
          apartmentId: ["Selectați un apartament."],
          temporaryPassword: ["Parola trebuie să aibă cel puțin 15 caractere."],
          confirmPassword: ["Parolele nu coincid."],
        }),
      )
      .mockRejectedValueOnce(
        new ApiError(409, "EMAIL_EXISTS", "Email already exists."),
      );

    render(
      <ResidentFormDialog
        open
        mode="create"
        apartments={apartments}
        onSubmit={onSubmit}
        onClose={vi.fn()}
      />,
    );

    await user.type(screen.getByLabelText("Prenume"), "Maria");
    await user.type(screen.getByLabelText("Nume"), "Ionescu");
    await user.type(screen.getByLabelText("Email"), "maria@example.test");
    await user.selectOptions(screen.getByLabelText("Apartament"), "apt-3a");
    await user.type(
      screen.getByLabelText("Parolă temporară"),
      "TemporaryPassword123!",
    );
    await user.type(
      screen.getByLabelText("Confirmă parola temporară"),
      "TemporaryPassword123!",
    );
    await user.click(screen.getByRole("button", { name: "Salvează locatarul" }));
    expect(await screen.findByText("Prenumele este obligatoriu.")).toBeVisible();
    expect(screen.getByText("Selectați un apartament.")).toBeVisible();
    expect(
      screen.getByText("Parola trebuie să aibă cel puțin 15 caractere."),
    ).toBeVisible();
    expect(screen.getByText("Parolele nu coincid.")).toBeVisible();

    await user.type(
      screen.getByLabelText("Parolă temporară"),
      "TemporaryPassword123!",
    );
    await user.type(
      screen.getByLabelText("Confirmă parola temporară"),
      "TemporaryPassword123!",
    );
    await user.click(screen.getByRole("button", { name: "Salvează locatarul" }));
    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Există deja un cont cu această adresă de email.",
    );
  });

  it("shows a safe network alert and clears credentials on cancel", async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    const onSubmit = vi.fn().mockRejectedValue(new TypeError("Failed to fetch"));

    render(
      <ResidentFormDialog
        open
        mode="create"
        apartments={apartments}
        onSubmit={onSubmit}
        onClose={onClose}
      />,
    );

    const password = screen.getByLabelText("Parolă temporară");
    await fillValidCreateForm(user);
    await user.click(screen.getByRole("button", { name: "Salvează locatarul" }));
    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Nu s-a putut salva locatarul. Verificați conexiunea și încercați din nou.",
    );

    await user.click(screen.getByRole("button", { name: "Anulează" }));
    expect(onClose).toHaveBeenCalledOnce();
    expect(password).toHaveValue("");
  });

  it("does not submit a temporary password shorter than 15 characters", async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn().mockResolvedValue(undefined);

    render(
      <ResidentFormDialog
        open
        mode="create"
        apartments={apartments}
        onSubmit={onSubmit}
        onClose={vi.fn()}
      />,
    );

    await fillValidCreateForm(user, "short-password");
    await user.click(screen.getByRole("button", { name: "Salvează locatarul" }));

    expect(onSubmit).not.toHaveBeenCalled();
  });

  it("traps focus and closes with Escape only when no request is in flight", async () => {
    const user = userEvent.setup();
    const pending = deferredPromise();
    const onClose = vi.fn();

    render(
      <ResidentFormDialog
        open
        mode="create"
        apartments={apartments}
        onSubmit={() => pending.promise}
        onClose={onClose}
      />,
    );

    await waitFor(() => expect(screen.getByLabelText("Prenume")).toHaveFocus());
    screen.getByRole("button", { name: "Închide" }).focus();
    await user.tab({ shift: true });
    expect(screen.getByRole("button", { name: "Salvează locatarul" })).toHaveFocus();
    await user.tab();
    expect(screen.getByRole("button", { name: "Închide" })).toHaveFocus();

    await user.keyboard("{Escape}");
    expect(onClose).toHaveBeenCalledOnce();

    await fillValidCreateForm(user);
    await user.click(screen.getByRole("button", { name: "Salvează locatarul" }));
    const dialog = screen.getByRole("dialog", { name: "Adaugă locatar" });
    expect(dialog).toHaveAttribute("aria-busy", "true");
    expect(dialog).toHaveFocus();
    expect(screen.getByRole("button", { name: "Se salvează…" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Anulează" })).toBeDisabled();
    await user.tab();
    expect(dialog).toHaveFocus();
    await user.keyboard("{Escape}");
    expect(onClose).toHaveBeenCalledOnce();

    pending.resolve();
  });
});

describe("ConfirmResidentDeactivation", () => {
  it("names the resident, explains the effect, and prevents duplicate confirmation", async () => {
    const user = userEvent.setup();
    const pending = deferredPromise();
    const onConfirm = vi.fn(() => pending.promise);

    render(
      <ConfirmResidentDeactivation
        open
        residentName="Ana Popescu"
        onConfirm={onConfirm}
        onClose={vi.fn()}
      />,
    );

    const dialog = screen.getByRole("dialog", {
      name: "Dezactivează locatarul",
    });
    expect(dialog).toHaveTextContent("Ana Popescu");
    expect(dialog).toHaveTextContent("sesiunile active");
    expect(dialog).toHaveTextContent("blochează autentificarea");
    expect(dialog).toHaveTextContent("fără a șterge istoricul");

    const confirm = screen.getByRole("button", { name: "Dezactivează" });
    await user.click(confirm);
    await user.click(confirm);

    expect(onConfirm).toHaveBeenCalledOnce();
    expect(dialog).toHaveAttribute("aria-busy", "true");
    expect(dialog).toHaveFocus();
    expect(confirm).toBeDisabled();
    expect(screen.getByRole("button", { name: "Anulează" })).toBeDisabled();
    await user.tab();
    expect(dialog).toHaveFocus();

    pending.resolve();
  });
});
