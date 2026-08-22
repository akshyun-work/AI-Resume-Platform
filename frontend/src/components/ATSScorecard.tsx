import React from 'react';
import { Award, Briefcase, Code, GraduationCap, Mail, CheckCircle2, AlertTriangle } from 'lucide-react';

interface ScoreBreakdownProps {
  score: number;
  breakdown: {
    contact_information: number;
    resume_sections: number;
    technical_skills: number;
    projects: number;
    experience: number;
    certifications: number;
    achievements: number;
  };
  skills: string[];
  name?: string | null;
  email?: string | null;
  phone?: string | null;
}

export const ATSScorecard: React.FC<ScoreBreakdownProps> = ({ score, breakdown, skills, name, email, phone }) => {
  const getScoreColor = (val: number) => {
    if (val >= 80) return 'text-emerald-400 border-emerald-500/30 bg-emerald-500/10';
    if (val >= 60) return 'text-amber-400 border-amber-500/30 bg-amber-500/10';
    return 'text-rose-400 border-rose-500/30 bg-rose-500/10';
  };

  return (
    <div className="space-y-6 w-full max-w-4xl">
      {/* Candidate Profile Bar */}
      <div className="bg-gray-900 border border-slate-800 p-4 rounded-xl flex flex-wrap justify-between items-center gap-4">
        <div>
          <span className="text-xs uppercase text-slate-400 tracking-wider">Candidate</span>
          <h3 className="text-lg font-bold text-white">{name || 'Unnamed Candidate'}</h3>
        </div>
        <div className="flex gap-4 text-xs text-slate-300">
          <span className="flex items-center gap-1.5"><Mail size={14} className="text-indigo-400"/> {email || 'Missing'}</span>
          <span className="flex items-center gap-1.5"><Mail size={14} className="text-indigo-400"/> {phone || 'Missing'}</span>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Score Radial */}
        <div className="bg-gray-900 border border-slate-800 p-6 rounded-2xl flex flex-col items-center justify-center text-center">
          <div className={`w-36 h-36 rounded-full border-4 flex flex-col items-center justify-center ${getScoreColor(score)}`}>
            <span className="text-4xl font-extrabold">{score}</span>
            <span className="text-xs uppercase tracking-wider font-semibold opacity-80">/ 100 ATS</span>
          </div>
          <h4 className="text-base font-semibold text-white mt-4 flex items-center gap-1.5">
            {score >= 80 ? <CheckCircle2 className="text-emerald-400" size={18}/> : <AlertTriangle className="text-amber-400" size={18}/>}
            {score >= 80 ? 'ATS Optimized' : score >= 60 ? 'Needs Improvement' : 'High Rejection Risk'}
          </h4>
        </div>

        {/* Categories */}
        <div className="lg:col-span-2 bg-gray-900 border border-slate-800 p-6 rounded-2xl space-y-3">
          <h4 className="text-xs font-semibold uppercase tracking-wider text-slate-400 mb-2">
            100-Point Algorithmic Evaluation
          </h4>
          {[
            { label: 'Technical Skills Matrix', val: breakdown.technical_skills, max: 25, icon: Code },
            { label: 'Essential Sections Found', val: breakdown.resume_sections, max: 15, icon: GraduationCap },
            { label: 'Projects Evaluation', val: breakdown.projects, max: 15, icon: Briefcase },
            { label: 'Professional Experience', val: breakdown.experience, max: 15, icon: Briefcase },
            { label: 'Contact Information Verified', val: breakdown.contact_information, max: 10, icon: Mail },
            { label: 'Certifications & Courses', val: breakdown.certifications, max: 10, icon: Award },
            { label: 'Achievements / Extracurricular', val: breakdown.achievements, max: 10, icon: Award },
          ].map((item, i) => (
            <div key={i} className="flex items-center gap-3">
              <item.icon size={15} className="text-slate-400 shrink-0" />
              <div className="flex-1">
                <div className="flex justify-between text-xs font-medium mb-1">
                  <span className="text-slate-300">{item.label}</span>
                  <span className="text-slate-400">{item.val} / {item.max}</span>
                </div>
                <div className="h-1.5 w-full bg-slate-800 rounded-full overflow-hidden">
                  <div
                    className="h-full bg-indigo-500 rounded-full"
                    style={{ width: `${(item.val / item.max) * 100}%` }}
                  />
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Skills Pill Cloud */}
      <div className="bg-gray-900 border border-slate-800 p-6 rounded-2xl">
        <h4 className="text-xs font-semibold uppercase tracking-wider text-slate-400 mb-3">
          Extracted Skills Dictionary Matches ({skills.length})
        </h4>
        <div className="flex flex-wrap gap-2">
          {skills.map((skill, index) => (
            <span key={index} className="px-3 py-1 rounded-lg text-xs font-medium bg-slate-800 text-indigo-300 border border-slate-700">
              {skill}
            </span>
          ))}
        </div>
      </div>
    </div>
  );
};
