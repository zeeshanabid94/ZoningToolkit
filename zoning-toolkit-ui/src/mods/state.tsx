import { create } from "zustand";

interface ModUIState {
    uiVisible: boolean
}

export const useModUIStore = create<ModUIState>((set) => ({
    uiVisible: false,
    updateUiVisible: (newValue: boolean) => set((state) => ({
        uiVisible: newValue
    }))
}))