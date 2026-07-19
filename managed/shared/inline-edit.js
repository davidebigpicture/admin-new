"use strict";

(function (global) {
    let activeEscapeComponent = null;

    function InlineEditController(options) {
        options = options || {};
        this.originalValue = options.value == null ? "" : options.value;
        this.value = this.originalValue;
        this.onSave = options.onSave || function () { return Promise.resolve(); };
        this.onError = options.onError || function () {};
        this.editing = false;
        this.pending = false;
        this.commitInFlight = false;
        this.suppressNextBlur = false;
    }

    InlineEditController.prototype.begin = function () {
        if (this.pending) {
            return false;
        }
        this.value = this.originalValue;
        this.editing = true;
        return true;
    };

    InlineEditController.prototype.cancel = function () {
        this.value = this.originalValue;
        this.editing = false;
        this.suppressNextBlur = true;
    };

    InlineEditController.prototype.handleBlur = function () {
        if (this.suppressNextBlur) {
            this.suppressNextBlur = false;
            return Promise.resolve(false);
        }
        return this.commit();
    };

    InlineEditController.prototype.commit = function () {
        const controller = this;
        if (!controller.editing || controller.pending || controller.commitInFlight) {
            return Promise.resolve(false);
        }
        if (controller.value === controller.originalValue) {
            controller.editing = false;
            return Promise.resolve(false);
        }

        controller.commitInFlight = true;
        controller.pending = true;
        return Promise.resolve(controller.onSave(controller.value, controller.originalValue))
            .then(function () {
                controller.originalValue = controller.value;
                controller.editing = false;
                return true;
            })
            .catch(function (error) {
                controller.value = controller.originalValue;
                controller.editing = false;
                controller.onError(error);
                throw error;
            })
            .finally(function () {
                controller.pending = false;
                controller.commitInFlight = false;
            });
    };

    function createInlineEditComponent() {
        return {
            name: "InlineEdit",
            props: {
                value: { type: [String, Number], default: "" },
                editorType: { type: String, default: "text" },
                options: { type: Array, default: function () { return []; } },
                searchable: { type: Boolean, default: false },
                commitOnChange: { type: Boolean, default: false },
                label: { type: String, required: true },
                editorId: { type: String, default: "" },
                placeholder: { type: String, default: "" },
                disabled: { type: Boolean, default: false },
                formatValue: { type: Function, default: function (value) { return value || ""; } },
                onSave: { type: Function, required: true },
                onError: { type: Function, default: function () {} }
            },
            emits: ["error"],
            data: function () {
                return {
                    controller: null,
                    escapeKeydownListener: null,
                    escapeKeydownTarget: null,
                    filterText: ""
                };
            },
            computed: {
                displayValue: function () {
                    return this.formatValue(this.controller ? this.controller.originalValue : this.value);
                },
                selectSize: function () {
                    const options = this.filteredOptions || this.options;
                    return Math.max(2, Math.min(options.length, 8));
                },
                filteredOptions: function () {
                    const filter = this.filterText.trim().toLowerCase();
                    if (!filter) {
                        return this.options;
                    }
                    return this.options.filter(function (option) {
                        return String(option.label || "").toLowerCase().indexOf(filter) >= 0 || String(option.value || "").toLowerCase().indexOf(filter) >= 0;
                    });
                }
            },
            watch: {
                value: function (value) {
                    if (this.controller && !this.controller.editing && !this.controller.pending) {
                        this.controller.originalValue = value == null ? "" : value;
                        this.controller.value = this.controller.originalValue;
                    }
                }
            },
            mounted: function () {
                const component = this;
                this.controller = global.Vue.reactive(new InlineEditController({
                    value: this.value,
                    onSave: this.onSave,
                    onError: this.reportError
                }));
                this.escapeKeydownListener = function (event) {
                    if (activeEscapeComponent === component && event.key === "Escape") {
                        event.preventDefault();
                        event.stopPropagation();
                        component.cancelEdit();
                    }
                };
            },
            beforeUnmount: function () {
                this.stopEscapeCapture();
            },
            methods: {
                reportError: function (error) {
                    this.onError(error);
                    this.$emit("error", error);
                },
                begin: function () {
                    if (!this.disabled && this.controller.begin()) {
                        if (activeEscapeComponent && activeEscapeComponent !== this) {
                            activeEscapeComponent.cancelEdit();
                        }
                        activeEscapeComponent = this;
                        this.filterText = "";
                        this.startEscapeCapture();
                        this.$forceUpdate();
                        this.$nextTick(function () {
                            const editor = this.searchable ? this.$refs.filter : this.$refs.editor;
                            if (editor) {
                                editor.focus();
                            }
                        });
                    }
                },
                commit: function () {
                    const component = this;
                    return this.controller.commit().catch(function () {
                        return false;
                    }).finally(function () {
                        component.stopEscapeCapture();
                        component.$forceUpdate();
                    });
                },
                startEscapeCapture: function () {
                    const target = global.document && typeof global.document.addEventListener === "function" ? global.document : global;
                    if (this.escapeKeydownTarget || !this.escapeKeydownListener || typeof target.addEventListener !== "function") {
                        return;
                    }
                    this.escapeKeydownTarget = target;
                    target.addEventListener("keydown", this.escapeKeydownListener, true);
                },
                stopEscapeCapture: function () {
                    if (this.escapeKeydownTarget) {
                        this.escapeKeydownTarget.removeEventListener("keydown", this.escapeKeydownListener, true);
                        this.escapeKeydownTarget = null;
                    }
                    if (activeEscapeComponent === this) {
                        activeEscapeComponent = null;
                    }
                },
                cancelEdit: function () {
                    if (!this.controller || !this.controller.editing) {
                        return;
                    }
                    this.controller.cancel();
                    this.stopEscapeCapture();
                    this.$forceUpdate();
                },
                onKeydown: function (event) {
                    if (!this.controller || !this.controller.editing) {
                        return;
                    }
                    if (event.key === "Escape") {
                        event.preventDefault();
                        this.cancelEdit();
                    } else if (event.key === "Enter" && this.editorType !== "textarea") {
                        event.preventDefault();
                        this.commit();
                    }
                },
                onBlur: function () {
                    const component = this;
                    this.controller.handleBlur().catch(function () {
                        return false;
                    }).finally(function () {
                        component.stopEscapeCapture();
                        component.$forceUpdate();
                    });
                },
                onFilterInput: function (event) {
                    this.filterText = event.target.value;
                },
                onSelectChange: function () {
                    if (this.commitOnChange) {
                        this.commit();
                    }
                },
                onCompositeFocusout: function () {
                    const component = this;
                    global.setTimeout(function () {
                        if (component.controller && component.controller.editing && component.$refs.composite && !component.$refs.composite.contains(global.document.activeElement)) {
                            component.onBlur();
                        }
                    }, 0);
                }
            },
            template: `
                <span class="admin-inline-edit" :class="{ 'admin-inline-edit--pending': controller && controller.pending }">
                    <button v-if="!controller || !controller.editing" type="button" class="admin-inline-edit__display" :disabled="disabled || (controller && controller.pending)" :aria-label="label" @click="begin"><span class="admin-inline-edit__display-label">{{ displayValue || placeholder }}</span><span class="admin-inline-edit__display-chevron" aria-hidden="true"></span></button>
                    <span v-else-if="editorType === 'select' && searchable" ref="composite" class="admin-inline-edit__searchable" @focusout="onCompositeFocusout">
                        <label class="sr-only" :for="(editorId || 'inlineEdit') + '-filter'">Filter {{ label }}</label>
                        <input :id="(editorId || 'inlineEdit') + '-filter'" ref="filter" :value="filterText" type="search" class="admin-inline-edit__input admin-inline-edit__filter" :placeholder="'Filter ' + label" :disabled="controller.pending" @input="onFilterInput" @keydown="onKeydown">
                        <select :id="editorId || null" ref="editor" v-model="controller.value" class="admin-inline-edit__input admin-inline-edit__select" :size="selectSize" :aria-label="label" :disabled="controller.pending" @change="onSelectChange" @keydown="onKeydown">
                            <option v-for="option in filteredOptions" :key="option.value" :value="option.value">{{ option.label }}</option>
                        </select>
                        <span v-if="filteredOptions.length === 0" class="admin-inline-edit__empty" role="status">No matching options.</span>
                    </span>
                    <select v-else-if="editorType === 'select'" :id="editorId || null" ref="editor" v-model="controller.value" class="admin-inline-edit__input admin-inline-edit__select" :size="selectSize" :aria-label="label" :disabled="controller.pending" @change="onSelectChange" @keydown="onKeydown" @blur="onBlur">
                        <option v-for="option in options" :key="option.value" :value="option.value">{{ option.label }}</option>
                    </select>
                    <textarea v-else-if="editorType === 'textarea'" :id="editorId || null" ref="editor" v-model="controller.value" class="admin-inline-edit__input admin-inline-edit__input--textarea" :placeholder="placeholder" :aria-label="label" :disabled="controller.pending" @keydown="onKeydown" @blur="onBlur"></textarea>
                    <input v-else :id="editorId || null" ref="editor" v-model="controller.value" class="admin-inline-edit__input" :type="editorType" :placeholder="placeholder" :aria-label="label" :disabled="controller.pending" @keydown="onKeydown" @blur="onBlur">
                    <span v-if="controller && controller.pending" class="admin-inline-edit__pending" aria-live="polite">Saving</span>
                </span>`
        };
    }

    const component = createInlineEditComponent();
    global.InlineEditController = InlineEditController;
    global.InlineEdit = component;
    if (typeof module !== "undefined" && module.exports) {
        module.exports = { InlineEditController: InlineEditController, InlineEdit: component };
    }
}(typeof window !== "undefined" ? window : global));