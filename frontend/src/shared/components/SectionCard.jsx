import React from 'react';

const SectionCard = ({ title, description, children, actions }) => (
  <section className="section-card">
    <div className="section-card-header">
      <div>
        <h2>{title}</h2>
        {description && <p>{description}</p>}
      </div>
      {actions && <div className="section-card-actions">{actions}</div>}
    </div>
    <div className="section-card-body">{children}</div>
  </section>
);

export default SectionCard;
