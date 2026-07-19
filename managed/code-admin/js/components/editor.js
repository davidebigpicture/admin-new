"use strict";

(function (global) {
    const viewModel = global.CodeAdminViewModel;

    global.CodeAdminComponents = global.CodeAdminComponents || {};
    global.CodeAdminComponents.Editor = {
        props: { editor: { type: Object, required: true }, selectedClass: { type: String, required: true }, workspace: { type: Object, required: true } },
        computed: {
            selectedClassInfo: function () { return viewModel.getSelectedClass(this.workspace, this.selectedClass); },
            selectedClassLabel: function () { const item = this.selectedClassInfo; return item ? item.codeClassDesc + " (" + item.codeClass + ")" : this.selectedClass; },
            detailFields: function () { return viewModel.getDetailFields({ fieldMetadata: { [this.selectedClass]: this.editor.fieldMetadata } }, this.selectedClass); }
        },
        watch: {
            "editor.codeValueError": function (errorMessage) { this.setCodeValueValidity(errorMessage); }
        },
        methods: {
            refreshMetadata: function () { this.$emit("refresh-metadata"); },
            setCodeValueValidity: function (errorMessage) { const input = this.$refs.codeValue; if (input) { input.setCustomValidity(errorMessage || ""); } },
            onCodeValueInput: function (event) { event.target.setCustomValidity(""); this.$emit("code-value-input"); }
        },
        template: `
            <section class="editor-panel code-admin-workspace panel panel-default" aria-labelledby="codeValueEditorTitle">
                <div class="panel-heading editor-panel-heading"><div><h3 id="codeValueEditorTitle" class="panel-title">{{ editor.mode === "edit" ? "Edit " + editor.codeValue : "Add code value" }}</h3><p class="editor-class-context">{{ selectedClassLabel }}</p></div><button type="button" class="btn btn-default btn-sm" @click="$emit('cancel')"><i class="fa fa-arrow-left" aria-hidden="true"></i> Back to list</button></div>
                <div class="panel-body"><form id="codeValueEditor" @submit.prevent="$emit('save')"><div class="editor-grid">
                    <div v-if="editor.mode === 'edit'" class="form-group"><label class="control-label">Value</label><p class="form-control-static code-value-static">{{ editor.codeValue }}</p></div>
                    <div v-if="editor.mode === 'edit'" class="form-group"><label class="control-label" for="codeValueRank">Rank</label><input id="codeValueRank" v-model.number="editor.orderBy" class="form-control" type="number" min="1" step="1" :disabled="editor.isProtected || editor.inactive || (editor.status && editor.status !== 'N')"></div>
                    <div v-else class="form-group" :class="{ 'has-error': editor.codeValueError }"><label class="control-label" for="codeValue">Value</label><input id="codeValue" ref="codeValue" v-model="editor.codeValue" class="form-control" pattern="[A-Za-z][A-Za-z0-9_-]*" title="Must begin with a letter and use only letters, numbers, hyphens, and underscores." :aria-invalid="editor.codeValueError ? 'true' : null" :aria-describedby="editor.codeValueError ? 'codeValueError' : null" required autofocus @input="onCodeValueInput" @blur="refreshMetadata"><span v-if="editor.codeValueError" id="codeValueError" class="help-block">{{ editor.codeValueError }}</span></div>
                    <div v-for="field in detailFields" :key="field.key" class="form-group" :class="{ 'editor-field-wide': field.controlType === 'textarea' }">
                        <label v-if="field.controlType !== 'radio'" class="control-label" :for="'detail-' + field.key">{{ field.label }}</label>
                        <textarea v-if="field.controlType === 'textarea'" :id="'detail-' + field.key" v-model="editor[field.key]" class="form-control" rows="4" :required="field.required"></textarea>
                        <fieldset v-else-if="field.controlType === 'radio'" class="code-admin-radio-options" :aria-required="field.required ? 'true' : 'false'"><legend>{{ field.label }}</legend><label v-for="(option, index) in field.options" :key="option.value" class="radio-inline" :for="'detail-' + field.key + '-' + index"><input :id="'detail-' + field.key + '-' + index" v-model="editor[field.key]" type="radio" :name="field.key" :value="option.value" :required="field.required && index === 0"> {{ option.label }}</label></fieldset>
                        <select v-else-if="field.controlType === 'select' || field.controlType === 'multiselect'" :id="'detail-' + field.key" v-model="editor[field.key]" class="form-control" :multiple="field.controlType === 'multiselect'" :required="field.required"><option v-if="field.controlType === 'select'" value=""></option><option v-for="option in field.options" :key="option.value" :value="option.value">{{ option.label }}</option></select>
                        <input v-else :id="'detail-' + field.key" v-model="editor[field.key]" class="form-control" :required="field.required">
                    </div>
                </div><div class="editor-actions"><button type="submit" class="btn btn-primary btn-sm"><i class="fa fa-check" aria-hidden="true"></i> Save</button><button type="button" class="btn btn-default btn-sm" @click="$emit('cancel')">Cancel</button></div></form></div>
            </section>`
    };
}(window));